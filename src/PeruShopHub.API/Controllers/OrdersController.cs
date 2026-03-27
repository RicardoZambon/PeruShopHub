using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Orders;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICostCalculationService _costService;

    public OrdersController(PeruShopHubDbContext db, ICostCalculationService costService)
    {
        _db = db;
        _costService = costService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string sortBy = "orderDate",
        [FromQuery] string sortDir = "desc")
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
        {
            query = query.Where(o => o.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(o => o.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(o => o.OrderDate <= dateTo.Value);
        }

        query = sortBy.ToLower() switch
        {
            "externalorderid" => sortDir == "asc" ? query.OrderBy(o => o.ExternalOrderId) : query.OrderByDescending(o => o.ExternalOrderId),
            "buyername" => sortDir == "asc" ? query.OrderBy(o => o.BuyerName) : query.OrderByDescending(o => o.BuyerName),
            "totalamount" => sortDir == "asc" ? query.OrderBy(o => o.TotalAmount) : query.OrderByDescending(o => o.TotalAmount),
            "profit" => sortDir == "asc" ? query.OrderBy(o => o.Profit) : query.OrderByDescending(o => o.Profit),
            "status" => sortDir == "asc" ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
            _ => sortDir == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListDto(
                o.Id,
                o.ExternalOrderId,
                o.BuyerName,
                o.ItemCount,
                o.TotalAmount,
                o.Profit,
                o.Status,
                o.OrderDate,
                o.TrackingNumber))
            .ToListAsync();

        return Ok(new PagedResult<OrderListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound();

        var buyer = new BuyerDto(
            order.BuyerName,
            order.BuyerNickname,
            order.BuyerEmail,
            order.BuyerPhone);

        var timeline = BuildTimeline(order.Status, order.OrderDate);

        var shipping = new ShippingInfoDto(
            order.TrackingNumber,
            order.Carrier,
            order.LogisticType,
            timeline);

        var paymentStatus = DerivePaymentStatus(order.Status);

        var payment = new PaymentInfoDto(
            order.PaymentMethod,
            order.Installments,
            order.PaymentAmount,
            paymentStatus);

        var items = order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.Name,
            i.Sku,
            i.Variation,
            i.Quantity,
            i.UnitPrice,
            i.Subtotal)).ToList();

        var costs = order.Costs.Select(c => new OrderCostDto(
            c.Id,
            c.Category,
            c.Description,
            c.Value,
            c.Source)).ToList();

        var revenue = order.TotalAmount;
        var totalCosts = order.Costs.Sum(c => c.Value);
        var profit = revenue - totalCosts;
        var margin = revenue > 0 ? (profit / revenue) * 100 : 0;

        var detail = new OrderDetailDto(
            order.Id,
            order.ExternalOrderId,
            buyer,
            order.ItemCount,
            order.TotalAmount,
            revenue,
            totalCosts,
            profit,
            margin,
            order.Status,
            order.OrderDate,
            shipping,
            payment,
            items,
            costs);

        return Ok(detail);
    }

    [HttpPost("{id:guid}/costs")]
    public async Task<ActionResult<OrderCostDto>> AddCost(Guid id, [FromBody] CreateOrderCostRequest request)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();

        var cost = new Core.Entities.OrderCost
        {
            Id = Guid.NewGuid(),
            OrderId = id,
            Category = request.Category,
            Description = request.Description,
            Value = request.Value,
            Source = "Manual"
        };

        _db.OrderCosts.Add(cost);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id },
            new OrderCostDto(cost.Id, cost.Category, cost.Description, cost.Value, cost.Source));
    }

    [HttpPut("{id:guid}/costs/{costId:guid}")]
    public async Task<ActionResult<OrderCostDto>> UpdateCost(Guid id, Guid costId, [FromBody] UpdateOrderCostRequest request)
    {
        var cost = await _db.OrderCosts.FirstOrDefaultAsync(c => c.Id == costId && c.OrderId == id);
        if (cost is null) return NotFound();

        cost.Category = request.Category;
        cost.Description = request.Description;
        cost.Value = request.Value;
        await _db.SaveChangesAsync();

        return Ok(new OrderCostDto(cost.Id, cost.Category, cost.Description, cost.Value, cost.Source));
    }

    [HttpDelete("{id:guid}/costs/{costId:guid}")]
    public async Task<IActionResult> DeleteCost(Guid id, Guid costId)
    {
        var cost = await _db.OrderCosts.FirstOrDefaultAsync(c => c.Id == costId && c.OrderId == id);
        if (cost is null) return NotFound();

        _db.OrderCosts.Remove(cost);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/recalculate-costs")]
    public async Task<IActionResult> RecalculateCosts(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync([id], ct);
        if (order is null) return NotFound();
        await _costService.RecalculateOrderCostsAsync(id, ct);
        return NoContent();
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
