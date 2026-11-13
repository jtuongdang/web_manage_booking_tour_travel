using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Settings;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> List(
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not int userId)
        {
            throw new UnauthorizedException("The authenticated user could not be resolved.");
        }

        var roles = currentUser.Roles.ToArray();
        var query = db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId || (n.TargetRole != null && roles.Contains(n.TargetRole)));
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query.OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new NotificationResponse(n.Id, n.Type, n.Title, n.Body, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(notifications);
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
        {
            throw new UnauthorizedException("The authenticated user could not be resolved.");
        }

        var roles = currentUser.Roles.ToArray();
        var notification = await db.Notifications.SingleOrDefaultAsync(
            n => n.Id == id && (n.UserId == userId || (n.TargetRole != null && roles.Contains(n.TargetRole))),
            cancellationToken);
        if (notification is null)
        {
            throw new NotFoundException("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }
}

public sealed record NotificationResponse(long Id, string Type, string Title, string Body, string? LinkUrl, bool IsRead, DateTime CreatedAt);
