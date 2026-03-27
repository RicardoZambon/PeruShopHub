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

    public async Task<IReadOnlyList<UserDetailDto>> GetListAsync(CancellationToken ct = default)
    {
        return await _db.SystemUsers
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UserDetailDto(
                u.Id, u.Name, u.Email, u.Role,
                u.IsActive, u.LastLogin, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.SystemUsers
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id, u.Name, u.Email, u.Role,
                u.IsActive, u.LastLogin, u.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email, ct))
            AddError(errors, "Email", "E-mail já está em uso.");

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

        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = NormalizeRole(request.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.SystemUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            user.Id, user.Name, user.Email, user.Role,
            user.IsActive, user.LastLogin, user.CreatedAt);
    }

    public async Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { id }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email && u.Id != id, ct))
            AddError(errors, "Email", "E-mail já está em uso.");

        if (string.IsNullOrWhiteSpace(request.Role))
            AddError(errors, "Role", "Perfil é obrigatório.");
        else if (!ValidRoles.Contains(request.Role))
            AddError(errors, "Role", "Perfil deve ser Admin, Manager ou Viewer.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.Role = NormalizeRole(request.Role);

        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            user.Id, user.Name, user.Email, user.Role,
            user.IsActive, user.LastLogin, user.CreatedAt);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { id }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
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
        // Normalize to match our canonical casing
        return role.Trim() switch
        {
            var r when r.Equals("Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            var r when r.Equals("Manager", StringComparison.OrdinalIgnoreCase) => "Manager",
            var r when r.Equals("Viewer", StringComparison.OrdinalIgnoreCase) => "Viewer",
            _ => role.Trim()
        };
    }
}
