using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Notifications;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public NotificationsController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetAll()
    {
        var notifications = await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Timestamp)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Description,
                n.Timestamp, n.IsRead, n.NavigationTarget))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification is null)
            return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _db.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return NoContent();
    }
}
