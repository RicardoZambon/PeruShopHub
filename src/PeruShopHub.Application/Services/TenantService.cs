using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class TenantService : ITenantService
{
    private readonly PeruShopHubDbContext _db;

    public TenantService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<TenantDetailDto> GetByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantDetailDto(
                t.Id, t.Name, t.Slug, t.IsActive,
                t.Members.Count, t.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        return tenant;
    }

    public async Task<TenantDetailDto> UpdateAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException("Name", "Nome da loja é obrigatório.");

        tenant.Name = request.Name.Trim();
        await _db.SaveChangesAsync(ct);

        var memberCount = await _db.TenantUsers.CountAsync(tu => tu.TenantId == tenantId, ct);
        return new TenantDetailDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, memberCount, tenant.CreatedAt);
    }

    public async Task<IReadOnlyList<TenantDetailDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantDetailDto(
                t.Id, t.Name, t.Slug, t.IsActive,
                t.Members.Count, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        tenant.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }
}
