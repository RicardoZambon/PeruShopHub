using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICostCalculationService _costService;

    public PurchaseOrderService(PeruShopHubDbContext db, ICostCalculationService costService)
    {
        _db = db;
        _costService = costService;
    }

    public async Task<PagedResult<PurchaseOrderListDto>> GetListAsync(
        int page, int pageSize, string? status, string? supplier,
        string sortBy, string sortDir, CancellationToken ct = default)
    {
        var query = _db.PurchaseOrders.AsNoTracking()
            .Include(po => po.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(po => po.Status == status);

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            var term = supplier.ToLower();
            query = query.Where(po => po.Supplier != null && po.Supplier.ToLower().Contains(term));
        }

        query = sortBy.ToLower() switch
        {
            "supplier" => sortDir == "desc" ? query.OrderByDescending(po => po.Supplier) : query.OrderBy(po => po.Supplier),
            "status" => sortDir == "desc" ? query.OrderByDescending(po => po.Status) : query.OrderBy(po => po.Status),
            "total" => sortDir == "desc" ? query.OrderByDescending(po => po.Total) : query.OrderBy(po => po.Total),
            _ => sortDir == "desc" ? query.OrderByDescending(po => po.CreatedAt) : query.OrderBy(po => po.CreatedAt),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderListDto(
                po.Id, po.Supplier, po.Status, po.Items.Count, po.Total, po.CreatedAt, po.ReceivedAt))
            .ToListAsync(ct);

        return new PagedResult<PurchaseOrderListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PurchaseOrderDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        return MapToDetailDto(po);
    }

    public async Task<PurchaseOrderDetailDto> CreateAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        ValidateCreateDto(dto);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Supplier = dto.Supplier,
            Notes = dto.Notes,
            Status = "Rascunho",
            CreatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            var item = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                ProductId = itemDto.ProductId,
                VariantId = itemDto.VariantId,
                Quantity = itemDto.Quantity,
                UnitCost = itemDto.UnitCost,
                TotalCost = itemDto.Quantity * itemDto.UnitCost
            };
            po.Items.Add(item);
        }

        po.Subtotal = po.Items.Sum(i => i.TotalCost);

        if (dto.Costs is { Count: > 0 })
        {
            foreach (var costDto in dto.Costs)
            {
                var cost = new PurchaseOrderCost
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    Description = costDto.Description,
                    Value = costDto.Value,
                    DistributionMethod = costDto.DistributionMethod
                };
                po.Costs.Add(cost);
            }
        }

        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(ct);

        // Reload with navigation properties for response
        var created = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id, ct);

        return MapToDetailDto(created);
    }

    public async Task<PurchaseOrderDetailDto> UpdateAsync(Guid id, CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        ValidateCreateDto(dto);

        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        if (po.Status != "Rascunho")
            throw new ConflictException("Somente pedidos com status 'Rascunho' podem ser editados.");

        po.Supplier = dto.Supplier;
        po.Notes = dto.Notes;

        // Replace items
        _db.PurchaseOrderItems.RemoveRange(po.Items);
        po.Items.Clear();

        foreach (var itemDto in dto.Items)
        {
            var item = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                ProductId = itemDto.ProductId,
                VariantId = itemDto.VariantId,
                Quantity = itemDto.Quantity,
                UnitCost = itemDto.UnitCost,
                TotalCost = itemDto.Quantity * itemDto.UnitCost
            };
            po.Items.Add(item);
        }

        // Replace costs
        _db.PurchaseOrderCosts.RemoveRange(po.Costs);
        po.Costs.Clear();

        if (dto.Costs is { Count: > 0 })
        {
            foreach (var costDto in dto.Costs)
            {
                var cost = new PurchaseOrderCost
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    Description = costDto.Description,
                    Value = costDto.Value,
                    DistributionMethod = costDto.DistributionMethod
                };
                po.Costs.Add(cost);
            }
        }

        po.Subtotal = po.Items.Sum(i => i.TotalCost);
        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        await _db.SaveChangesAsync(ct);

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id, ct);

        return MapToDetailDto(updated);
    }

    public async Task<PurchaseOrderDetailDto> ReceiveAsync(Guid id, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        if (po.Status == "Recebido")
            throw new ConflictException("Este pedido de compra já foi recebido.");

        await _costService.ReceivePurchaseOrderAsync(id);

        var received = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == id, ct);

        return MapToDetailDto(received);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.FindAsync(new object[] { id }, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        if (po.Status != "Rascunho")
            throw new AppValidationException("Status", "Apenas ordens em rascunho podem ser canceladas.");

        po.Status = "Cancelado";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PurchaseOrderDetailDto> AddCostAsync(Guid id, CreatePurchaseOrderCostDto dto, CancellationToken ct = default)
    {
        ValidateCostDto(dto);

        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        var cost = new PurchaseOrderCost
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            Description = dto.Description,
            Value = dto.Value,
            DistributionMethod = dto.DistributionMethod
        };

        po.Costs.Add(cost);
        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        await _db.SaveChangesAsync(ct);

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id, ct);

        return MapToDetailDto(updated);
    }

    public async Task<PurchaseOrderDetailDto> UpdateCostAsync(Guid id, Guid costId, CreatePurchaseOrderCostDto dto, CancellationToken ct = default)
    {
        ValidateCostDto(dto);

        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        var cost = po.Costs.FirstOrDefault(c => c.Id == costId);
        if (cost is null)
            throw new NotFoundException("PurchaseOrderCost", costId);

        cost.Description = dto.Description;
        cost.Value = dto.Value;
        cost.DistributionMethod = dto.DistributionMethod;

        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        await _db.SaveChangesAsync(ct);

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id, ct);

        return MapToDetailDto(updated);
    }

    public async Task<PurchaseOrderDetailDto> RemoveCostAsync(Guid id, Guid costId, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        var cost = po.Costs.FirstOrDefault(c => c.Id == costId);
        if (cost is null)
            throw new NotFoundException("PurchaseOrderCost", costId);

        po.Costs.Remove(cost);
        _db.PurchaseOrderCosts.Remove(cost);

        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        await _db.SaveChangesAsync(ct);

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id, ct);

        return MapToDetailDto(updated);
    }

    public async Task<CostDistributionPreviewDto> GetCostPreviewAsync(Guid id, decimal value, string method, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
            throw new NotFoundException("PurchaseOrder", id);

        var allocations = new List<ItemAllocationDto>();

        if (method == "by_quantity")
        {
            var totalQuantity = po.Items.Sum(i => i.Quantity);
            foreach (var item in po.Items)
            {
                var allocated = totalQuantity > 0
                    ? value * ((decimal)item.Quantity / totalQuantity)
                    : 0m;
                var effectiveUnit = item.Quantity > 0
                    ? (item.TotalCost + allocated) / item.Quantity
                    : 0m;
                allocations.Add(new ItemAllocationDto(
                    item.Id, item.Product.Name, item.Variant.Sku,
                    Math.Round(allocated, 4), Math.Round(effectiveUnit, 4)));
            }
        }
        else // by_value
        {
            var totalCost = po.Items.Sum(i => i.TotalCost);
            foreach (var item in po.Items)
            {
                var allocated = totalCost > 0
                    ? value * (item.TotalCost / totalCost)
                    : 0m;
                var effectiveUnit = item.Quantity > 0
                    ? (item.TotalCost + allocated) / item.Quantity
                    : 0m;
                allocations.Add(new ItemAllocationDto(
                    item.Id, item.Product.Name, item.Variant.Sku,
                    Math.Round(allocated, 4), Math.Round(effectiveUnit, 4)));
            }
        }

        return new CostDistributionPreviewDto(allocations);
    }

    // --- Private helpers ---

    private static void ValidateCreateDto(CreatePurchaseOrderDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Supplier))
            errors.Add("Supplier", new List<string> { "Fornecedor é obrigatório." });

        if (dto.Items is null || dto.Items.Count == 0)
        {
            errors.Add("Items", new List<string> { "O pedido deve conter pelo menos um item." });
        }
        else
        {
            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                if (item.ProductId == Guid.Empty)
                    errors.TryAdd($"Items[{i}].ProductId", new List<string> { "ProductId é obrigatório." });
                if (item.VariantId == Guid.Empty)
                    errors.TryAdd($"Items[{i}].VariantId", new List<string> { "VariantId é obrigatório." });
                if (item.Quantity <= 0)
                    errors.TryAdd($"Items[{i}].Quantity", new List<string> { "Quantidade deve ser maior que zero." });
                if (item.UnitCost < 0)
                    errors.TryAdd($"Items[{i}].UnitCost", new List<string> { "Custo unitário não pode ser negativo." });
            }
        }

        if (dto.Costs is { Count: > 0 })
        {
            for (int i = 0; i < dto.Costs.Count; i++)
            {
                ValidateSingleCost(dto.Costs[i], $"Costs[{i}]", errors);
            }
        }

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private static void ValidateCostDto(CreatePurchaseOrderCostDto dto)
    {
        var errors = new Dictionary<string, List<string>>();
        ValidateSingleCost(dto, "", errors);

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private static void ValidateSingleCost(CreatePurchaseOrderCostDto dto, string prefix, Dictionary<string, List<string>> errors)
    {
        var descKey = string.IsNullOrEmpty(prefix) ? "Description" : $"{prefix}.Description";
        var valKey = string.IsNullOrEmpty(prefix) ? "Value" : $"{prefix}.Value";
        var methKey = string.IsNullOrEmpty(prefix) ? "DistributionMethod" : $"{prefix}.DistributionMethod";

        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.TryAdd(descKey, new List<string> { "Descrição é obrigatória." });
        if (dto.Value < 0)
            errors.TryAdd(valKey, new List<string> { "Valor não pode ser negativo." });
        if (dto.DistributionMethod != "by_value" && dto.DistributionMethod != "by_quantity")
            errors.TryAdd(methKey, new List<string> { "Método de distribuição deve ser 'by_value' ou 'by_quantity'." });
    }

    private static void RecalculateAllocations(PurchaseOrder po)
    {
        var items = po.Items.ToList();
        if (items.Count == 0) return;

        // Reset allocations
        foreach (var item in items)
        {
            item.AllocatedAdditionalCost = 0;
        }

        foreach (var cost in po.Costs)
        {
            if (cost.DistributionMethod == "by_quantity")
            {
                var totalQuantity = items.Sum(i => i.Quantity);
                if (totalQuantity <= 0) continue;
                foreach (var item in items)
                {
                    item.AllocatedAdditionalCost += cost.Value * ((decimal)item.Quantity / totalQuantity);
                }
            }
            else // by_value
            {
                var totalCost = items.Sum(i => i.TotalCost);
                if (totalCost <= 0) continue;
                foreach (var item in items)
                {
                    item.AllocatedAdditionalCost += cost.Value * (item.TotalCost / totalCost);
                }
            }
        }

        foreach (var item in items)
        {
            item.AllocatedAdditionalCost = Math.Round(item.AllocatedAdditionalCost, 4);
            item.EffectiveUnitCost = item.Quantity > 0
                ? Math.Round((item.TotalCost + item.AllocatedAdditionalCost) / item.Quantity, 4)
                : 0m;
        }
    }

    private static PurchaseOrderDetailDto MapToDetailDto(PurchaseOrder po)
    {
        return new PurchaseOrderDetailDto(
            po.Id, po.Supplier, po.Status, po.Notes,
            po.Subtotal, po.AdditionalCosts, po.Total,
            po.CreatedAt, po.ReceivedAt,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id, i.ProductId, i.VariantId,
                i.Product.Name, i.Variant.Sku,
                i.Quantity, i.UnitCost, i.TotalCost,
                i.AllocatedAdditionalCost, i.EffectiveUnitCost)).ToList(),
            po.Costs.Select(c => new PurchaseOrderCostDto(
                c.Id, c.Description, c.Value, c.DistributionMethod)).ToList());
    }
}
