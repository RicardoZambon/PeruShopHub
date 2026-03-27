using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICostCalculationService _costService;

    public PurchaseOrdersController(PeruShopHubDbContext db, ICostCalculationService costService)
    {
        _db = db;
        _costService = costService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? supplier = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc")
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

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderListDto(
                po.Id, po.Supplier, po.Status, po.Items.Count, po.Total, po.CreatedAt, po.ReceivedAt))
            .ToListAsync();

        return Ok(new PagedResult<PurchaseOrderListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id)
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

        var detail = MapToDetailDto(po);
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Create([FromBody] CreatePurchaseOrderDto dto)
    {
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
        await _db.SaveChangesAsync();

        // Reload with navigation properties for response
        var created = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id);

        return CreatedAtAction(nameof(GetById), new { id = po.Id }, MapToDetailDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Update(Guid id, [FromBody] CreatePurchaseOrderDto dto)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

        if (po.Status != "Rascunho")
            return Conflict(new { message = "Somente pedidos com status 'Rascunho' podem ser editados." });

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

        await _db.SaveChangesAsync();

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id);

        return Ok(MapToDetailDto(updated));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Receive(Guid id)
    {
        var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

        if (po.Status == "Recebido")
            return Conflict(new { message = "Este pedido de compra já foi recebido." });

        await _costService.ReceivePurchaseOrderAsync(id);

        var received = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == id);

        return Ok(MapToDetailDto(received));
    }

    [HttpPost("{id:guid}/costs")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> AddCost(Guid id, [FromBody] CreatePurchaseOrderCostDto dto)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

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

        await _db.SaveChangesAsync();

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id);

        return Ok(MapToDetailDto(updated));
    }

    [HttpDelete("{id:guid}/costs/{costId:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> RemoveCost(Guid id, Guid costId)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Costs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

        var cost = po.Costs.FirstOrDefault(c => c.Id == costId);
        if (cost is null)
            return NotFound();

        po.Costs.Remove(cost);
        _db.PurchaseOrderCosts.Remove(cost);

        po.AdditionalCosts = po.Costs.Sum(c => c.Value);
        po.Total = po.Subtotal + po.AdditionalCosts;

        RecalculateAllocations(po);

        await _db.SaveChangesAsync();

        var updated = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .Include(p => p.Costs)
            .FirstAsync(p => p.Id == po.Id);

        return Ok(MapToDetailDto(updated));
    }

    [HttpGet("{id:guid}/cost-preview")]
    public async Task<ActionResult<CostDistributionPreviewDto>> CostPreview(
        Guid id,
        [FromQuery] decimal value,
        [FromQuery] string method = "by_value")
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
            return NotFound();

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

        return Ok(new CostDistributionPreviewDto(allocations));
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po is null) return NotFound();
        if (po.Status != "Rascunho")
            return BadRequest(new { message = "Apenas ordens em rascunho podem ser canceladas." });

        po.Status = "Cancelado";
        await _db.SaveChangesAsync();
        return NoContent();
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
