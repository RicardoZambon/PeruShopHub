using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Messages;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMarketplaceMessageService _service;

    public MessagesController(IMarketplaceMessageService service)
    {
        _service = service;
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<MessageThreadDto>> GetThread(Guid orderId, CancellationToken ct = default)
    {
        var result = await _service.GetThreadByOrderAsync(orderId, ct);
        return Ok(result);
    }

    [HttpPost("orders/{orderId:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<MessageDto>> SendMessage(
        Guid orderId, [FromBody] SendMessageRequest request, CancellationToken ct = default)
    {
        var result = await _service.SendMessageAsync(orderId, request, ct);
        return Ok(result);
    }

    [HttpPost("orders/{orderId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid orderId, CancellationToken ct = default)
    {
        await _service.MarkAsReadAsync(orderId, ct);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken ct = default)
    {
        var result = await _service.GetUnreadCountAsync(ct);
        return Ok(result);
    }
}
