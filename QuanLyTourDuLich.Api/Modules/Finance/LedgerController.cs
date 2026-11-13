using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Finance;

[ApiController]
[Route("api/v1/ledger")]
[Authorize(Roles = RoleCodes.Accountant + "," + RoleCodes.Admin)]
public sealed class LedgerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LedgerReport>> Get(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? direction,
        [FromQuery] string? category,
        [FromQuery] int? departureId,
        CancellationToken cancellationToken)
    {
        var query = db.FinancialLedgers.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(direction)) query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (departureId.HasValue) query = query.Where(x => x.DepartureId == departureId.Value);

        var entries = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
        var income = entries.Where(x => x.Direction == "In").Sum(x => x.Amount);
        var expenses = entries.Where(x => x.Direction == "Out").Sum(x => x.Amount);
        return Ok(new LedgerReport(entries, income, expenses, income - expenses));
    }
}

public sealed record LedgerReport(IReadOnlyList<FinancialLedgers> Entries, decimal TotalIn, decimal TotalOut, decimal NetCashflow);
