using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Orders;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class OrderService : IOrderService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICostCalculationService _costService;
    private readonly IAuditService _auditService;

    public OrderService(PeruShopHubDbContext db, ICostCalculationService costService, IAuditService auditService)
    {
        _db = db;
        _costService = costService;
        _auditService = auditService;
    }

    public async Task<PagedResult<OrderListDto>> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateTime? dateFrom, DateTime? dateTo,
        string sortBy, string sortDir, CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o =>
                o.ExternalOrderId.ToLower().Contains(term) ||
                o.BuyerName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (dateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(o => o.OrderDate <= dateTo.Value);

        query = sortBy.ToLower() switch
        {
            "externalorderid" => sortDir == "asc" ? query.OrderBy(o => o.ExternalOrderId) : query.OrderByDescending(o => o.ExternalOrderId),
            "buyername" => sortDir == "asc" ? query.OrderBy(o => o.BuyerName) : query.OrderByDescending(o => o.BuyerName),
            "totalamount" => sortDir == "asc" ? query.OrderBy(o => o.TotalAmount) : query.OrderByDescending(o => o.TotalAmount),
            "profit" => sortDir == "asc" ? query.OrderBy(o => o.Profit) : query.OrderByDescending(o => o.Profit),
            "status" => sortDir == "asc" ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
            _ => sortDir == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListDto(
                o.Id, o.ExternalOrderId, o.BuyerName, o.ItemCount,
                o.TotalAmount, o.Profit, o.Status, o.IsFulfilled,
                o.OrderDate, o.TrackingNumber))
            .ToListAsync(ct);

        return new PagedResult<OrderListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            throw new NotFoundException("Pedido", id);

        var buyer = new BuyerDto(
            order.BuyerName, order.BuyerNickname,
            order.BuyerEmail, order.BuyerPhone);

        var timeline = BuildTimeline(order.Status, order.OrderDate);

        var shipping = new ShippingInfoDto(
            order.TrackingNumber, order.Carrier,
            order.LogisticType, timeline);

        var paymentStatus = DerivePaymentStatus(order.Status);

        var payment = new PaymentInfoDto(
            order.PaymentMethod, order.Installments,
            order.PaymentAmount, paymentStatus);

        var items = order.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.Name, i.Sku, i.Variation,
            i.Quantity, i.UnitPrice, i.Subtotal)).ToList();

        var costs = order.Costs.Select(c => new OrderCostDto(
            c.Id, c.Category, c.Description, c.Value, c.Source)).ToList();

        var revenue = order.TotalAmount;
        var totalCosts = order.Costs.Sum(c => c.Value);
        var profit = revenue - totalCosts;
        var margin = revenue > 0 ? (profit / revenue) * 100 : 0;

        return new OrderDetailDto(
            order.Id, order.ExternalOrderId, buyer, order.ItemCount,
            order.TotalAmount, revenue, totalCosts, profit, margin,
            order.Status, order.IsFulfilled, order.FulfilledAt, order.OrderDate,
            shipping, payment, items, costs);
    }

    public async Task<OrderCostDto> AddCostAsync(Guid orderId, CreateOrderCostRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync([orderId], ct);
        if (order is null)
            throw new NotFoundException("Pedido", orderId);

        ValidateCostRequest(request.Category, request.Value);

        var cost = new Core.Entities.OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Category = request.Category,
            Description = request.Description,
            Value = request.Value,
            Source = "Manual"
        };

        _db.OrderCosts.Add(cost);
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync("Custo adicionado ao pedido", "Order", orderId,
            null, new { cost.Category, cost.Description, cost.Value }, ct);

        return new OrderCostDto(cost.Id, cost.Category, cost.Description, cost.Value, cost.Source);
    }

    public async Task<OrderCostDto> UpdateCostAsync(Guid orderId, Guid costId, UpdateOrderCostRequest request, CancellationToken ct = default)
    {
        var cost = await _db.OrderCosts.FirstOrDefaultAsync(c => c.Id == costId && c.OrderId == orderId, ct);
        if (cost is null)
            throw new NotFoundException("Custo do pedido", costId);

        ValidateCostRequest(request.Category, request.Value);

        var oldValues = new { cost.Category, cost.Description, cost.Value };
        cost.Category = request.Category;
        cost.Description = request.Description;
        cost.Value = request.Value;
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync("Custo do pedido atualizado", "Order", orderId,
            oldValues, new { cost.Category, cost.Description, cost.Value }, ct);

        return new OrderCostDto(cost.Id, cost.Category, cost.Description, cost.Value, cost.Source);
    }

    public async Task DeleteCostAsync(Guid orderId, Guid costId, CancellationToken ct = default)
    {
        var cost = await _db.OrderCosts.FirstOrDefaultAsync(c => c.Id == costId && c.OrderId == orderId, ct);
        if (cost is null)
            throw new NotFoundException("Custo do pedido", costId);

        await _auditService.LogAsync("Custo do pedido removido", "Order", orderId,
            new { cost.Category, cost.Description, cost.Value }, null, ct);

        _db.OrderCosts.Remove(cost);
        await _db.SaveChangesAsync(ct);
    }

    public async Task FulfillAsync(Guid orderId, CancellationToken ct = default)
    {
        await _costService.FulfillOrderAsync(orderId, ct);
    }

    public async Task RecalculateCostsAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync([orderId], ct);
        if (order is null)
            throw new NotFoundException("Pedido", orderId);

        await _costService.RecalculateOrderCostsAsync(orderId, ct);
    }

    private static void ValidateCostRequest(string category, decimal value)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(category))
            errors["Category"] = ["Categoria é obrigatória"];

        if (value <= 0)
            errors["Value"] = ["Valor deve ser maior que zero"];

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private static IReadOnlyList<TimelineStepDto> BuildTimeline(string status, DateTime orderDate)
    {
        var isNotCancelled = status != "Cancelado";
        var isShippedOrDelivered = status == "Enviado" || status == "Entregue";
        var isDelivered = status == "Entregue";

        return new List<TimelineStepDto>
        {
            new("Pedido realizado", orderDate, isNotCancelled ? "Concluido" : "Concluido"),
            new("Pagamento aprovado", isNotCancelled ? orderDate : null, isNotCancelled ? "Concluido" : "Cancelado"),
            new("Enviado", isShippedOrDelivered ? (DateTime?)null : null, isShippedOrDelivered ? "Concluido" : "Pendente"),
            new("Entregue", isDelivered ? (DateTime?)null : null, isDelivered ? "Concluido" : "Pendente"),
        };
    }

    private static string DerivePaymentStatus(string orderStatus)
    {
        return orderStatus switch
        {
            "Pago" or "Enviado" or "Entregue" => "Aprovado",
            "Cancelado" => "Cancelado",
            "Devolvido" => "Devolvido",
            _ => "Pendente"
        };
    }
}
