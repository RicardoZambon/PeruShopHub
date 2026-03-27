using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Supplies;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class SupplyService : ISupplyService
{
    private readonly PeruShopHubDbContext _db;
    private readonly INotificationDispatcher _dispatcher;

    public SupplyService(PeruShopHubDbContext db, INotificationDispatcher dispatcher)
    {
        _db = db;
        _dispatcher = dispatcher;
    }

    public async Task<PagedResult<SupplyListDto>> GetListAsync(
        int page, int pageSize, string? search,
        string? category, string? status,
        string sortBy, string sortDir,
        CancellationToken ct = default)
    {
        var query = _db.Supplies.AsNoTracking().Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Sku.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        query = sortBy.ToLower() switch
        {
            "sku" => sortDir == "desc" ? query.OrderByDescending(s => s.Sku) : query.OrderBy(s => s.Sku),
            "unitcost" => sortDir == "desc" ? query.OrderByDescending(s => s.UnitCost) : query.OrderBy(s => s.UnitCost),
            "stock" => sortDir == "desc" ? query.OrderByDescending(s => s.Stock) : query.OrderBy(s => s.Stock),
            "category" => sortDir == "desc" ? query.OrderByDescending(s => s.Category) : query.OrderBy(s => s.Category),
            "status" => sortDir == "desc" ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => sortDir == "desc" ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplyListDto(
                s.Id, s.Name, s.Sku, s.Category, s.UnitCost,
                s.Stock, s.MinimumStock, s.Supplier, s.Status))
            .ToListAsync(ct);

        return new PagedResult<SupplyListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SupplyDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var supply = await _db.Supplies
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct)
            ?? throw new NotFoundException("Insumo", id);

        return new SupplyDetailDto(
            supply.Id, supply.Name, supply.Sku, supply.Category,
            supply.UnitCost, supply.Stock, supply.MinimumStock,
            supply.Supplier, supply.Status, supply.IsActive,
            supply.CreatedAt, supply.UpdatedAt,
            supply.Version);
    }

    public async Task<SupplyListDto> CreateAsync(CreateSupplyDto dto, CancellationToken ct = default)
    {
        Validate(dto);

        var supply = new Supply
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Sku = dto.Sku,
            Category = dto.Category,
            UnitCost = dto.UnitCost,
            Stock = dto.Stock,
            MinimumStock = dto.MinimumStock,
            Supplier = dto.Supplier,
            Status = "Ativo",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Supplies.Add(supply);
        await _db.SaveChangesAsync(ct);

        await _dispatcher.BroadcastDataChangeAsync("supply", "created", supply.Id.ToString(), ct);

        return new SupplyListDto(
            supply.Id, supply.Name, supply.Sku, supply.Category,
            supply.UnitCost, supply.Stock, supply.MinimumStock,
            supply.Supplier, supply.Status);
    }

    public async Task<SupplyListDto> UpdateAsync(Guid id, UpdateSupplyDto dto, CancellationToken ct = default)
    {
        var supply = await _db.Supplies.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct)
            ?? throw new NotFoundException("Insumo", id);

        ValidateUpdate(dto);

        _db.Entry(supply).Property(s => s.Version).OriginalValue = dto.Version;

        if (dto.Name is not null) supply.Name = dto.Name;
        if (dto.Sku is not null) supply.Sku = dto.Sku;
        if (dto.Category is not null) supply.Category = dto.Category;
        if (dto.UnitCost.HasValue) supply.UnitCost = dto.UnitCost.Value;
        if (dto.Stock.HasValue) supply.Stock = dto.Stock.Value;
        if (dto.MinimumStock.HasValue) supply.MinimumStock = dto.MinimumStock.Value;
        if (dto.Supplier is not null) supply.Supplier = dto.Supplier;
        if (dto.Status is not null) supply.Status = dto.Status;
        supply.UpdatedAt = DateTime.UtcNow;
        supply.Version++;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException();
        }

        await _dispatcher.BroadcastDataChangeAsync("supply", "updated", supply.Id.ToString(), ct);

        return new SupplyListDto(
            supply.Id, supply.Name, supply.Sku, supply.Category,
            supply.UnitCost, supply.Stock, supply.MinimumStock,
            supply.Supplier, supply.Status);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var supply = await _db.Supplies.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct)
            ?? throw new NotFoundException("Insumo", id);

        supply.IsActive = false;
        supply.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _dispatcher.BroadcastDataChangeAsync("supply", "deleted", supply.Id.ToString(), ct);
    }

    private static void Validate(CreateSupplyDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório."];
        else if (dto.Name.Length > 200)
            errors["Name"] = ["Nome deve ter no máximo 200 caracteres."];

        if (dto.Stock < 0)
            errors["Stock"] = ["Estoque não pode ser negativo."];

        if (dto.MinimumStock < 0)
            errors["MinimumStock"] = ["Estoque mínimo não pode ser negativo."];

        if (dto.UnitCost < 0)
            errors["UnitCost"] = ["Custo unitário não pode ser negativo."];

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private static void ValidateUpdate(UpdateSupplyDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                errors["Name"] = ["Nome é obrigatório."];
            else if (dto.Name.Length > 200)
                errors["Name"] = ["Nome deve ter no máximo 200 caracteres."];
        }

        if (dto.Stock.HasValue && dto.Stock.Value < 0)
            errors["Stock"] = ["Estoque não pode ser negativo."];

        if (dto.MinimumStock.HasValue && dto.MinimumStock.Value < 0)
            errors["MinimumStock"] = ["Estoque mínimo não pode ser negativo."];

        if (dto.UnitCost.HasValue && dto.UnitCost.Value < 0)
            errors["UnitCost"] = ["Custo unitário não pode ser negativo."];

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }
}
