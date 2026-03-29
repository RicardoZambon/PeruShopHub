using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IAuditService _auditService;
    private readonly IStockSyncService _stockSyncService;

    public InventoryService(PeruShopHubDbContext db, IAuditService auditService, IStockSyncService stockSyncService)
    {
        _db = db;
        _auditService = auditService;
        _stockSyncService = stockSyncService;
    }

    public async Task<PagedResult<InventoryItemDto>> GetOverviewAsync(
        int page, int pageSize, string? search,
        string sortBy, string sortDir,
        CancellationToken ct = default)
    {
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(ct);

        var projected = query.Select(p => new InventoryItemDto(
            p.Id,
            p.Sku,
            p.Name,
            p.Variants.Sum(v => v.Stock),
            0,
            p.Variants.Sum(v => v.Stock),
            p.PurchaseCost,
            p.Variants.Sum(v => v.Stock) * p.PurchaseCost,
            p.MinStock,
            p.MaxStock));

        var desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        projected = sortBy.ToLower() switch
        {
            "sku" => desc ? projected.OrderByDescending(i => i.Sku) : projected.OrderBy(i => i.Sku),
            "totalstock" => desc ? projected.OrderByDescending(i => i.TotalStock) : projected.OrderBy(i => i.TotalStock),
            "reserved" => desc ? projected.OrderByDescending(i => i.Reserved) : projected.OrderBy(i => i.Reserved),
            "available" => desc ? projected.OrderByDescending(i => i.Available) : projected.OrderBy(i => i.Available),
            "unitcost" => desc ? projected.OrderByDescending(i => i.UnitCost) : projected.OrderBy(i => i.UnitCost),
            "stockvalue" => desc ? projected.OrderByDescending(i => i.StockValue) : projected.OrderBy(i => i.StockValue),
            _ => desc ? projected.OrderByDescending(i => i.ProductName) : projected.OrderBy(i => i.ProductName),
        };

        var items = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<InventoryItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        Guid? productId, Guid? variantId, string? type,
        DateTime? dateFrom, DateTime? dateTo, string? createdBy,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildMovementsQuery(productId, variantId, type, dateFrom, dateTo, createdBy);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementDto(
                m.Id,
                m.Variant != null ? m.Variant.Sku : m.Product.Sku,
                m.Product.Name,
                m.Type,
                m.Quantity,
                m.UnitCost,
                m.Reason,
                m.CreatedBy,
                m.CreatedAt,
                m.PurchaseOrderId,
                m.OrderId))
            .ToListAsync(ct);

        return new PagedResult<StockMovementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<byte[]> ExportMovementsToExcelAsync(
        Guid? productId, Guid? variantId, string? type,
        DateTime? dateFrom, DateTime? dateTo, string? createdBy,
        CancellationToken ct = default)
    {
        var query = BuildMovementsQuery(productId, variantId, type, dateFrom, dateTo, createdBy);

        var movements = await query
            .Select(m => new StockMovementDto(
                m.Id,
                m.Variant != null ? m.Variant.Sku : m.Product.Sku,
                m.Product.Name,
                m.Type,
                m.Quantity,
                m.UnitCost,
                m.Reason,
                m.CreatedBy,
                m.CreatedAt,
                m.PurchaseOrderId,
                m.OrderId))
            .ToListAsync(ct);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Movimentações");

        // Headers
        ws.Cell(1, 1).Value = "Data";
        ws.Cell(1, 2).Value = "SKU";
        ws.Cell(1, 3).Value = "Produto";
        ws.Cell(1, 4).Value = "Tipo";
        ws.Cell(1, 5).Value = "Quantidade";
        ws.Cell(1, 6).Value = "Custo Unitário";
        ws.Cell(1, 7).Value = "Observação";
        ws.Cell(1, 8).Value = "Usuário";

        var headerRange = ws.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1A237E");
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

        for (int i = 0; i < movements.Count; i++)
        {
            var m = movements[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = m.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 2).Value = m.Sku;
            ws.Cell(row, 3).Value = m.ProductName;
            ws.Cell(row, 4).Value = m.Type;
            ws.Cell(row, 5).Value = m.Quantity;
            ws.Cell(row, 6).Value = m.UnitCost ?? 0;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = m.Reason ?? "";
            ws.Cell(row, 8).Value = m.CreatedBy ?? "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private IQueryable<StockMovement> BuildMovementsQuery(
        Guid? productId, Guid? variantId, string? type,
        DateTime? dateFrom, DateTime? dateTo, string? createdBy)
    {
        var query = _db.StockMovements.AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.Variant)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        if (variantId.HasValue)
            query = query.Where(m => m.VariantId == variantId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(m => m.Type == type);

        if (dateFrom.HasValue)
            query = query.Where(m => m.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(m => m.CreatedAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            var cb = createdBy.Trim().ToLower();
            query = query.Where(m => m.CreatedBy != null && m.CreatedBy.ToLower().Contains(cb));
        }

        return query.OrderByDescending(m => m.CreatedAt);
    }

    public async Task<StockMovementDto> CreateMovementAsync(StockAdjustmentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new AppValidationException("Reason", "Motivo é obrigatório para ajustes manuais.");

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.ProductId == dto.ProductId, ct)
            ?? throw new NotFoundException("Variante do produto não encontrada.");

        variant.Stock += dto.Quantity;

        // Adjust allocations if new stock is below total allocated
        await AdjustAllocationsIfExceedingAsync(variant.Id, variant.Stock, ct);

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            Type = "Ajuste",
            Quantity = dto.Quantity,
            UnitCost = variant.PurchaseCost ?? variant.Product.PurchaseCost,
            Reason = dto.Reason,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync("Ajuste de estoque", "ProductVariant", variant.Id,
            new { Stock = variant.Stock - dto.Quantity },
            new { Stock = variant.Stock, Quantity = dto.Quantity, Reason = dto.Reason }, ct);

        // Enqueue ML stock sync if variant is linked to a listing
        await _stockSyncService.EnqueueVariantSyncAsync(variant.TenantId, variant.Id, ct);

        return new StockMovementDto(
            movement.Id,
            variant.Sku,
            variant.Product.Name,
            movement.Type,
            movement.Quantity,
            movement.UnitCost,
            movement.Reason,
            movement.CreatedBy,
            movement.CreatedAt,
            movement.PurchaseOrderId,
            movement.OrderId);
    }

    public async Task<ProductAllocationsDto> GetAllocationsAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Produto não encontrado.");

        var variantIds = product.Variants.Select(v => v.Id).ToList();

        var allocations = await _db.StockAllocations.AsNoTracking()
            .Where(a => variantIds.Contains(a.ProductVariantId))
            .ToListAsync(ct);

        var variantDtos = product.Variants.Select(v =>
        {
            var variantAllocations = allocations
                .Where(a => a.ProductVariantId == v.Id)
                .Select(a => new StockAllocationDto(
                    a.Id,
                    a.ProductVariantId,
                    v.Sku,
                    a.MarketplaceId,
                    a.AllocatedQuantity,
                    a.ReservedQuantity))
                .ToList();

            var totalAllocated = variantAllocations.Sum(a => a.AllocatedQuantity);

            return new VariantAllocationsDto(
                v.Id,
                v.Sku,
                v.Stock,
                totalAllocated,
                v.Stock - totalAllocated,
                variantAllocations);
        }).ToList();

        return new ProductAllocationsDto(product.Id, product.Name, variantDtos);
    }

    public async Task<StockAllocationDto> UpdateAllocationAsync(Guid variantId, UpdateStockAllocationDto dto, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new NotFoundException("Variante do produto não encontrada.");

        if (dto.AllocatedQuantity < 0)
            throw new AppValidationException("AllocatedQuantity", "Quantidade alocada não pode ser negativa.");

        // Check total allocations for this variant (excluding the marketplace being updated)
        var existingAllocations = await _db.StockAllocations
            .Where(a => a.ProductVariantId == variantId && a.MarketplaceId != dto.MarketplaceId)
            .SumAsync(a => a.AllocatedQuantity, ct);

        if (existingAllocations + dto.AllocatedQuantity > variant.Stock)
            throw new AppValidationException("AllocatedQuantity",
                $"Soma das alocações ({existingAllocations + dto.AllocatedQuantity}) excede o estoque total ({variant.Stock}).");

        var allocation = await _db.StockAllocations
            .FirstOrDefaultAsync(a => a.ProductVariantId == variantId && a.MarketplaceId == dto.MarketplaceId, ct);

        if (allocation == null)
        {
            allocation = new StockAllocation
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variantId,
                MarketplaceId = dto.MarketplaceId,
                AllocatedQuantity = dto.AllocatedQuantity,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.StockAllocations.Add(allocation);
        }
        else
        {
            allocation.AllocatedQuantity = dto.AllocatedQuantity;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Enqueue ML stock sync when ML allocation changes
        if (dto.MarketplaceId == "mercadolivre")
            await _stockSyncService.EnqueueVariantSyncAsync(variant.TenantId, variant.Id, ct);

        return new StockAllocationDto(
            allocation.Id,
            allocation.ProductVariantId,
            variant.Sku,
            allocation.MarketplaceId,
            allocation.AllocatedQuantity,
            allocation.ReservedQuantity);
    }

    public async Task<IReadOnlyList<StockAlertDto>> GetAlertsAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsActive && p.MinStock != null)
            .ToListAsync(ct);

        var alerts = new List<StockAlertDto>();
        foreach (var p in products)
        {
            var totalStock = p.Variants.Sum(v => v.Stock);
            if (totalStock <= p.MinStock!.Value)
            {
                alerts.Add(new StockAlertDto(
                    p.Id, p.Sku, p.Name,
                    totalStock, p.MinStock, p.MinStock.Value - totalStock));
            }
        }

        return alerts.OrderBy(a => a.TotalStock).ToList();
    }

    public async Task<ReconciliationResultDto> ReconcileAsync(ReconciliationRequestDto dto, string createdBy, CancellationToken ct = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new AppValidationException("Items", "Nenhum item informado para reconciliação.");

        var errors = new Dictionary<string, List<string>>();

        // Validate no duplicate variants
        var duplicates = dto.Items.GroupBy(i => i.VariantId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            errors.Add("Items", new List<string> { "Itens duplicados encontrados na lista." });

        // Validate no negative quantities
        var negatives = dto.Items.Where(i => i.CountedQuantity < 0).ToList();
        if (negatives.Count > 0)
            errors.Add("CountedQuantity", new List<string> { "Quantidade contada não pode ser negativa." });

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var variantIds = dto.Items.Select(i => i.VariantId).ToList();
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(ct);

        if (variants.Count != variantIds.Count)
        {
            var found = variants.Select(v => v.Id).ToHashSet();
            var missing = variantIds.Where(id => !found.Contains(id)).ToList();
            throw new NotFoundException($"Variantes não encontradas: {string.Join(", ", missing)}");
        }

        var batchId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var resultItems = new List<ReconciliationResultItemDto>();
        var discrepancies = 0;
        var totalDifference = 0;

        foreach (var item in dto.Items)
        {
            var variant = variants.First(v => v.Id == item.VariantId);
            var systemQty = variant.Stock;
            var difference = item.CountedQuantity - systemQty;
            var hasDiscrepancy = difference != 0;

            if (hasDiscrepancy)
            {
                discrepancies++;
                totalDifference += Math.Abs(difference);

                variant.Stock = item.CountedQuantity;

                // Adjust allocations if new stock is below total allocated
                await AdjustAllocationsIfExceedingAsync(variant.Id, variant.Stock, ct);

                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = variant.ProductId,
                    VariantId = variant.Id,
                    Type = "Ajuste",
                    Quantity = difference,
                    UnitCost = variant.PurchaseCost ?? variant.Product.PurchaseCost,
                    Reason = $"Reconciliação física (lote {batchId})",
                    CreatedBy = createdBy,
                    CreatedAt = now
                });
            }

            resultItems.Add(new ReconciliationResultItemDto(
                variant.Id,
                variant.Sku,
                variant.Product.Name,
                systemQty,
                item.CountedQuantity,
                difference,
                hasDiscrepancy));
        }

        await _db.SaveChangesAsync(ct);

        // Enqueue ML stock sync for all variants with discrepancies
        foreach (var resultItem in resultItems.Where(r => r.HasDiscrepancy))
        {
            var variant = variants.First(v => v.Id == resultItem.VariantId);
            await _stockSyncService.EnqueueVariantSyncAsync(variant.TenantId, variant.Id, ct);
        }

        return new ReconciliationResultDto(
            batchId,
            dto.Items.Count,
            discrepancies,
            totalDifference,
            now,
            resultItems);
    }

    private async Task AdjustAllocationsIfExceedingAsync(Guid variantId, int newStock, CancellationToken ct)
    {
        var allocations = await _db.StockAllocations
            .Where(a => a.ProductVariantId == variantId)
            .ToListAsync(ct);

        if (allocations.Count == 0) return;

        var totalAllocated = allocations.Sum(a => a.AllocatedQuantity);
        if (totalAllocated <= newStock) return;

        // Proportionally reduce allocations to fit new stock
        var targetTotal = Math.Max(0, newStock);
        foreach (var alloc in allocations)
        {
            alloc.AllocatedQuantity = totalAllocated > 0
                ? (int)Math.Floor((double)alloc.AllocatedQuantity / totalAllocated * targetTotal)
                : 0;
            alloc.UpdatedAt = DateTime.UtcNow;
        }

        // Distribute remainder due to rounding to the largest allocation
        var distributed = allocations.Sum(a => a.AllocatedQuantity);
        var remainder = targetTotal - distributed;
        if (remainder > 0 && allocations.Count > 0)
        {
            allocations.OrderByDescending(a => a.AllocatedQuantity).First().AllocatedQuantity += remainder;
        }
    }
}
