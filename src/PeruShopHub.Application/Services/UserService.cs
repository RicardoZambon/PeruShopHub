using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class UserService : IUserService
{
    private readonly PeruShopHubDbContext _db;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Manager", "Viewer"
    };

    public UserService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDetailDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.User)
            .Where(tu => tu.TenantId == tenantId)
            .OrderBy(tu => tu.User.Name)
            .Select(tu => new UserDetailDto(
                tu.UserId, tu.User.Name, tu.User.Email, tu.Role,
                tu.User.IsActive, tu.User.LastLogin, tu.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<UserDetailDto> InviteMemberAsync(Guid tenantId, CreateUserRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");

        if (string.IsNullOrWhiteSpace(request.Password))
            AddError(errors, "Password", "Senha é obrigatória.");
        else if (request.Password.Length < 8)
            AddError(errors, "Password", "Senha deve ter no mínimo 8 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Role))
            AddError(errors, "Role", "Perfil é obrigatório.");
        else if (!ValidRoles.Contains(request.Role))
            AddError(errors, "Role", "Perfil deve ser Admin, Manager ou Viewer.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _db.SystemUsers.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (existingUser is not null)
        {
            var existingMembership = await _db.TenantUsers
                .AnyAsync(tu => tu.TenantId == tenantId && tu.UserId == existingUser.Id, ct);

            if (existingMembership)
                throw new AppValidationException("Email", "Usuário já é membro desta loja.");

            var membership = new TenantUser
            {
                TenantId = tenantId,
                UserId = existingUser.Id,
                Role = NormalizeRole(request.Role),
                CreatedAt = DateTime.UtcNow
            };
            _db.TenantUsers.Add(membership);
            await _db.SaveChangesAsync(ct);

            return new UserDetailDto(
                existingUser.Id, existingUser.Name, existingUser.Email,
                membership.Role, existingUser.IsActive, existingUser.LastLogin, membership.CreatedAt);
        }

        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newMembership = new TenantUser
        {
            TenantId = tenantId,
            UserId = user.Id,
            Role = NormalizeRole(request.Role),
            CreatedAt = DateTime.UtcNow
        };

        _db.SystemUsers.Add(user);
        _db.TenantUsers.Add(newMembership);
        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            user.Id, user.Name, user.Email,
            newMembership.Role, user.IsActive, user.LastLogin, newMembership.CreatedAt);
    }

    public async Task<UserDetailDto> UpdateMemberAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var membership = await _db.TenantUsers
            .Include(tu => tu.User)
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId, ct)
            ?? throw new NotFoundException("Membro", userId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant() && u.Id != userId, ct))
            AddError(errors, "Email", "E-mail já está em uso.");

        if (string.IsNullOrWhiteSpace(request.Role))
            AddError(errors, "Role", "Perfil é obrigatório.");
        else if (!ValidRoles.Contains(request.Role) && !request.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            AddError(errors, "Role", "Perfil deve ser Owner, Admin, Manager ou Viewer.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        membership.User.Name = request.Name.Trim();
        membership.User.Email = request.Email.Trim().ToLowerInvariant();
        membership.Role = NormalizeRole(request.Role);

        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            membership.UserId, membership.User.Name, membership.User.Email,
            membership.Role, membership.User.IsActive, membership.User.LastLogin, membership.CreatedAt);
    }

    public async Task RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var membership = await _db.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId, ct)
            ?? throw new NotFoundException("Membro", userId);

        if (membership.Role == "Owner")
            throw new AppValidationException("Role", "Não é possível remover o proprietário da loja.");

        _db.TenantUsers.Remove(membership);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { id }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            AddError(errors, "NewPassword", "Nova senha é obrigatória.");
        else if (request.NewPassword.Length < 8)
            AddError(errors, "NewPassword", "Nova senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            AddError(errors, "CurrentPassword", "Senha atual é obrigatória.");
        else if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            AddError(errors, "CurrentPassword", "Senha atual incorreta.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            AddError(errors, "NewPassword", "Nova senha é obrigatória.");
        else if (request.NewPassword.Length < 8)
            AddError(errors, "NewPassword", "Nova senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync(ct);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim() switch
        {
            var r when r.Equals("Owner", StringComparison.OrdinalIgnoreCase) => "Owner",
            var r when r.Equals("Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            var r when r.Equals("Manager", StringComparison.OrdinalIgnoreCase) => "Manager",
            var r when r.Equals("Viewer", StringComparison.OrdinalIgnoreCase) => "Viewer",
            _ => role.Trim()
        };
    }
}
