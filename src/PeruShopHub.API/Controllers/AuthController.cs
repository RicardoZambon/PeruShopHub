using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Email;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly IConfiguration _config;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        PeruShopHubDbContext db,
        IConfiguration config,
        IUserService userService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _config = config;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "E-mail ou senha incorretos." });

        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.Tenant.IsActive);

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.ShopName))
            AddError(errors, "ShopName", "Nome da loja é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!Regex.IsMatch(request.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant()))
            AddError(errors, "Email", "E-mail já está em uso.");

        if (string.IsNullOrWhiteSpace(request.Password))
            AddError(errors, "Password", "Senha é obrigatória.");
        else if (request.Password.Length < 8)
            AddError(errors, "Password", "Senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var slug = GenerateSlug(request.ShopName);
        var baseSlug = slug;
        var counter = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.ShopName.Trim(),
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new TenantUser
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow,
            Tenant = tenant,
            User = user
        };

        _db.Tenants.Add(tenant);
        _db.SystemUsers.Add(user);
        _db.TenantUsers.Add(membership);

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        user.LastLogin = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _ = SendWelcomeEmailAsync(user.Email, user.Name);

        return Created("", new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken && u.IsActive);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Token expirado. Faça login novamente." });

        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.Tenant.IsActive);

        var accessToken = GenerateAccessToken(user, membership);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            newRefreshToken,
            BuildUserDto(user, membership)));
    }

    [Authorize]
    [HttpGet("tenants")]
    public async Task<ActionResult<List<TenantSummaryDto>>> GetMyTenants()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tenants = await _db.TenantUsers
            .Include(tu => tu.Tenant)
            .Where(tu => tu.UserId == userId && tu.Tenant.IsActive)
            .Select(tu => new TenantSummaryDto(tu.TenantId, tu.Tenant.Name, tu.Tenant.Slug, tu.Role))
            .ToListAsync();

        return Ok(tenants);
    }

    [Authorize]
    [HttpPost("switch-tenant")]
    public async Task<ActionResult<AuthResponse>> SwitchTenant([FromBody] SwitchTenantRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.TenantId == request.TenantId && m.Tenant.IsActive);

        if (membership is null && !user.IsSuperAdmin)
            return Forbid();

        // For super-admin without membership, create a virtual admin context
        if (membership is null && user.IsSuperAdmin)
        {
            var tenant = await _db.Tenants.FindAsync(request.TenantId);
            if (tenant is null) return NotFound();
            membership = new TenantUser
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = "Admin",
                Tenant = tenant,
                User = user
            };
        }

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userId, out var id))
        {
            var user = await _db.SystemUsers.FindAsync(id);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                await _db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var tenantId = User.FindFirstValue("tenant_id");
        return Ok(new UserDto(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue("name") ?? "",
            User.FindFirstValue(ClaimTypes.Email) ?? "",
            User.FindFirstValue("tenant_role"),
            tenantId is not null ? Guid.Parse(tenantId) : null,
            User.FindFirstValue("tenant_name"),
            User.FindFirstValue("is_super_admin") == "true"));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.ChangePasswordAsync(userId, request, ct);
        return NoContent();
    }

    private async Task SendWelcomeEmailAsync(string email, string userName)
    {
        try
        {
            var dashboardUrl = $"{Request.Scheme}://{Request.Host}/dashboard";

            var body = EmailTemplateBuilder.Paragraph($"Olá, <strong>{userName}</strong>! 👋") +
                       EmailTemplateBuilder.Paragraph("Seja bem-vindo(a) ao <strong>PeruShopHub</strong>! Sua conta foi criada com sucesso.") +
                       EmailTemplateBuilder.Heading("Próximos passos") +
                       EmailTemplateBuilder.List(
                           "Conecte sua conta do Mercado Livre em <strong>Configurações → Marketplaces</strong>",
                           "Importe seus produtos para começar o acompanhamento",
                           "Confira o relatório de lucratividade no Dashboard"
                       ) +
                       EmailTemplateBuilder.Button("Acessar o Dashboard", dashboardUrl) +
                       EmailTemplateBuilder.Paragraph("Se tiver dúvidas, estamos aqui para ajudar. Boas vendas!");

            var html = EmailTemplateBuilder.BuildLayout("Bem-vindo ao PeruShopHub!", body);

            var plainText = $"""
                Olá, {userName}!

                Seja bem-vindo(a) ao PeruShopHub! Sua conta foi criada com sucesso.

                Próximos passos:
                - Conecte sua conta do Mercado Livre em Configurações → Marketplaces
                - Importe seus produtos para começar o acompanhamento
                - Confira o relatório de lucratividade no Dashboard

                Acesse o Dashboard: {dashboardUrl}

                Se tiver dúvidas, estamos aqui para ajudar. Boas vendas!
                """;

            await _emailService.SendAsync(email, "Bem-vindo ao PeruShopHub!", html, textBody: plainText);
            _logger.LogInformation("Welcome email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", email);
        }
    }

    private string GenerateAccessToken(SystemUser user, TenantUser? membership)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("name", user.Name),
            new("is_super_admin", user.IsSuperAdmin.ToString().ToLowerInvariant()),
        };

        if (membership is not null)
        {
            claims.Add(new Claim("tenant_id", membership.TenantId.ToString()));
            claims.Add(new Claim("tenant_role", membership.Role));
            claims.Add(new Claim("tenant_name", membership.Tenant?.Name ?? ""));
            claims.Add(new Claim(ClaimTypes.Role, membership.Role));
        }

        if (user.IsSuperAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        var expMinutes = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto BuildUserDto(SystemUser user, TenantUser? membership)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            membership?.Role,
            membership?.TenantId,
            membership?.Tenant?.Name,
            user.IsSuperAdmin);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[áàãâä]", "a");
        slug = Regex.Replace(slug, @"[éèêë]", "e");
        slug = Regex.Replace(slug, @"[íìîï]", "i");
        slug = Regex.Replace(slug, @"[óòõôö]", "o");
        slug = Regex.Replace(slug, @"[úùûü]", "u");
        slug = Regex.Replace(slug, @"[ç]", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }
}
