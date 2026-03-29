using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.DTOs.Profile;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class UserService : IUserService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ILogger<UserService> _logger;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Manager", "Viewer"
    };

    public UserService(PeruShopHubDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Usuário", userId);

        return MapToProfileDto(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("Usuário", userId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.Name = request.Name.Trim();
        await _db.SaveChangesAsync(ct);

        return MapToProfileDto(user);
    }

    public async Task<ProfileDto> UpdateProfileEmailAsync(Guid userId, UpdateProfileEmailRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("Usuário", userId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            AddError(errors, "CurrentPassword", "Senha atual é obrigatória para alterar o e-mail.");
        else if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            AddError(errors, "CurrentPassword", "Senha atual incorreta.");

        if (string.IsNullOrWhiteSpace(request.NewEmail))
            AddError(errors, "NewEmail", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.NewEmail))
            AddError(errors, "NewEmail", "E-mail inválido.");
        else
        {
            var email = request.NewEmail.Trim().ToLowerInvariant();
            if (await _db.SystemUsers.AnyAsync(u => u.Email == email && u.Id != userId, ct))
                AddError(errors, "NewEmail", "E-mail já está em uso.");
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.Email = request.NewEmail.Trim().ToLowerInvariant();
        await _db.SaveChangesAsync(ct);

        return MapToProfileDto(user);
    }

    public async Task<ProfileDto> UpdateProfileAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("Usuário", userId);

        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync(ct);

        return MapToProfileDto(user);
    }

    public async Task RemoveProfileAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("Usuário", userId);

        user.AvatarUrl = null;
        await _db.SaveChangesAsync(ct);
    }

    private static ProfileDto MapToProfileDto(SystemUser user)
    {
        return new ProfileDto(user.Id, user.Name, user.Email, user.AvatarUrl, user.LastLogin, user.CreatedAt);
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
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

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
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

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AccountDeletionDto> RequestAccountDeletionAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("Usuário", userId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Password))
            AddError(errors, "Password", "Senha é obrigatória.");
        else if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            AddError(errors, "Password", "Senha incorreta.");

        if (string.IsNullOrWhiteSpace(request.ConfirmPhrase) ||
            !request.ConfirmPhrase.Equals("EXCLUIR MINHA CONTA", StringComparison.OrdinalIgnoreCase))
            AddError(errors, "ConfirmPhrase", "Digite 'EXCLUIR MINHA CONTA' para confirmar.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        // Check for existing pending deletion
        var existing = await _db.AccountDeletionRequests
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == "Pending", ct);

        if (existing is not null)
            return MapToDeletionDto(existing);

        // Check if user is the only owner of any tenant
        var ownerships = await _db.TenantUsers
            .Where(tu => tu.UserId == userId && tu.Role == "Owner")
            .Select(tu => tu.TenantId)
            .ToListAsync(ct);

        foreach (var tenantId in ownerships)
        {
            var otherOwners = await _db.TenantUsers
                .CountAsync(tu => tu.TenantId == tenantId && tu.Role == "Owner" && tu.UserId != userId, ct);

            if (otherOwners == 0)
            {
                var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
                AddError(errors, "Account",
                    $"Você é o único proprietário da loja '{tenant?.Name}'. Transfira a propriedade ou exclua a loja antes de excluir sua conta.");
            }
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var deletion = new AccountDeletionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ScheduledDeletionAt = DateTime.UtcNow.AddDays(30),
        };

        _db.AccountDeletionRequests.Add(deletion);

        // Deactivate account immediately
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Account deletion requested for user {UserId}, scheduled for {ScheduledAt}",
            userId, deletion.ScheduledDeletionAt);

        return MapToDeletionDto(deletion);
    }

    public async Task CancelAccountDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var deletion = await _db.AccountDeletionRequests
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == "Pending", ct)
            ?? throw new NotFoundException("Solicitação de exclusão", userId);

        deletion.Status = "Cancelled";
        deletion.CancelledAt = DateTime.UtcNow;

        // Reactivate account
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct);
        if (user is not null)
            user.IsActive = true;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Account deletion cancelled for user {UserId}", userId);
    }

    public async Task<AccountDeletionDto?> GetPendingDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var deletion = await _db.AccountDeletionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Status == "Pending", ct);

        return deletion is null ? null : MapToDeletionDto(deletion);
    }

    public async Task ProcessExpiredDeletionsAsync(CancellationToken ct = default)
    {
        var expiredDeletions = await _db.AccountDeletionRequests
            .Include(d => d.User)
            .Where(d => d.Status == "Pending" && d.ScheduledDeletionAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var deletion in expiredDeletions)
        {
            try
            {
                var user = deletion.User;

                // Remove all tenant memberships
                var memberships = await _db.TenantUsers
                    .Where(tu => tu.UserId == user.Id)
                    .ToListAsync(ct);
                _db.TenantUsers.RemoveRange(memberships);

                // Anonymize user data
                user.Email = $"deleted_{user.Id:N}@anonymized.local";
                user.Name = "Usuário Excluído";
                user.PasswordHash = string.Empty;
                user.AvatarUrl = null;
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                user.IsActive = false;

                deletion.Status = "Completed";
                deletion.CompletedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Account hard-deleted (anonymized) for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process account deletion for user {UserId}", deletion.UserId);
            }
        }
    }

    private static AccountDeletionDto MapToDeletionDto(AccountDeletionRequest deletion)
    {
        return new AccountDeletionDto(
            deletion.Id, deletion.Status, deletion.CreatedAt,
            deletion.ScheduledDeletionAt, deletion.CancelledAt);
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
