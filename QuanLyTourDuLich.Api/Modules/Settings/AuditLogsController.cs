using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Data;

namespace QuanLyTourDuLich.Api.Modules.Settings;

[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> List(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(log => log.EntityName.Contains(search) || log.EntityId.Contains(search) || log.Action.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var entries = await query.OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id)
            .Skip((request.NormalizedPageIndex - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .Select(log => new AuditLogResponse(
                log.Id, log.ActorUserId, log.ActorRole, log.Action, log.EntityName,
                log.EntityId, log.OldValueJson, log.NewValueJson, log.IpAddress, log.UserAgent, log.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResult<AuditLogResponse>(entries, request.NormalizedPageIndex, request.NormalizedPageSize, total));
    }
}

public sealed record AuditLogResponse(long Id, int? ActorUserId, string? ActorRole, string Action, string EntityName, string EntityId, string? OldValueJson, string? NewValueJson, string? IpAddress, string? UserAgent, DateTime CreatedAt);
