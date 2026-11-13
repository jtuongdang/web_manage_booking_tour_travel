using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

[ApiController]
[Route("api/v1/bookings")]
public sealed class BookingsController(AppDbContext db, BookingService bookings, ICurrentUserService current, IAuditLogService audit) : ControllerBase
{
    [HttpPost, AllowAnonymous]
    public async Task<ActionResult<Bookings>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        if (current.IsAuthenticated && !current.Roles.Any(r => r is RoleCodes.Sales or RoleCodes.Admin or RoleCodes.Operator) && request.BookingChannel != "WebB2C")
            throw new ForbiddenException("Only staff may create an assisted booking.");
        var result = await bookings.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { codeOrId = result.BookingCode }, result);
    }

    [HttpGet("my"), Authorize(Roles = RoleCodes.Customer)]
    public async Task<ActionResult<IReadOnlyList<Bookings>>> Mine(CancellationToken ct) => Ok(await db.Bookings.AsNoTracking().Where(x => x.CustomerId == current.CustomerId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct));

    [HttpGet, Authorize(Roles = RoleCodes.Sales + "," + RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<ActionResult<PagedResult<Bookings>>> List([FromQuery] PagedRequest page, [FromQuery] string? status, CancellationToken ct)
    {
        var query = db.Bookings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(page.Search)) { var s = page.Search.Trim(); query = query.Where(x => x.BookingCode.Contains(s) || x.ContactName.Contains(s) || x.ContactPhone.Contains(s) || x.ContactEmail.Contains(s)); }
        var count = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page.NormalizedPageIndex - 1) * page.NormalizedPageSize).Take(page.NormalizedPageSize).ToListAsync(ct);
        return Ok(new PagedResult<Bookings>(items, page.NormalizedPageIndex, page.NormalizedPageSize, count));
    }

    [HttpGet("{codeOrId}")]
    public async Task<ActionResult<Bookings>> Get(string codeOrId, [FromQuery] string? phone, [FromQuery] string? email, CancellationToken ct)
    {
        var query = db.Bookings.AsNoTracking().AsQueryable();
        if (long.TryParse(codeOrId, out var id)) query = query.Where(x => x.Id == id); else query = query.Where(x => x.BookingCode == codeOrId);
        var item = await query.SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Booking not found.");
        var staff = current.Roles.Any(x => RoleCodes.InternalStaff.Contains(x));
        if (!staff && current.CustomerId != item.CustomerId && !(phone == item.ContactPhone && !string.IsNullOrWhiteSpace(phone)) && !(email != null && string.Equals(email, item.ContactEmail, StringComparison.OrdinalIgnoreCase)))
            throw new ForbiddenException("Provide the verified contact phone/email or sign in as the booking owner.");
        return Ok(item);
    }

    [HttpPut("{id:long}/passengers"), Authorize]
    public async Task<IActionResult> UpdatePassengers(long id, IReadOnlyList<PassengerRequest> passengers, CancellationToken ct)
    {
        var booking = await bookings.GetAsync(id, ct); EnsureAccess(booking);
        var departure = await db.TourDepartures.SingleAsync(x => x.Id == booking.DepartureId, ct);
        if (departure.CutoffAt < DateTime.UtcNow) throw new ConflictException("Departure cutoff has passed.");
        var expected = booking.AdultCount + booking.ChildCount + booking.InfantCount;
        if (passengers.Count != expected || passengers.Count(x => x.PaxType == "Adult") != booking.AdultCount || passengers.Count(x => x.PaxType == "Child") != booking.ChildCount || passengers.Count(x => x.PaxType == "Infant") != booking.InfantCount)
            throw new BadRequestException("Passenger manifest must match the booking counts.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var old = await db.BookingPassengers.Where(x => x.BookingId == id).ToListAsync(ct); db.BookingPassengers.RemoveRange(old);
        db.BookingPassengers.AddRange(passengers.Select(p => new BookingPassengers { BookingId = id, PaxType = p.PaxType, FullName = p.FullName.Trim(), Nationality = p.Nationality, Gender = p.Gender, DateOfBirth = p.DateOfBirth, IdentityNumber = p.IdentityNumber, PassportExpiryDate = p.PassportExpiryDate, SpecialRequest = p.SpecialRequest, CheckInStatus = "Pending", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }));
        await db.SaveChangesAsync(ct); await audit.WriteAsync("UpdatePassengers", "Bookings", id.ToString(), old.Count, passengers.Count, current.UserId, cancellationToken: ct); await tx.CommitAsync(ct); return NoContent();
    }

    [HttpPatch("{id:long}/adjust-pax"), Authorize(Roles = RoleCodes.Sales + "," + RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Adjust(long id, AdjustPaxRequest request, CancellationToken ct) { await bookings.AdjustAsync(id, request, ct); return NoContent(); }

    [HttpPost("{id:long}/reschedule"), Authorize]
    public async Task<IActionResult> Reschedule(long id, RescheduleRequest request, CancellationToken ct) { EnsureAccess(await bookings.GetAsync(id, ct)); await bookings.RescheduleAsync(id, request, ct); return NoContent(); }

    [HttpPost("{id:long}/cancel-request"), Authorize]
    public async Task<IActionResult> CancelRequest(long id, CancelRequest request, CancellationToken ct) { await bookings.RequestCancelAsync(id, request.Reason, ct); return NoContent(); }

    [HttpPost("{id:long}/approve-cancel"), Authorize(Roles = RoleCodes.Sales + "," + RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> ApproveCancel(long id, CancellationToken ct) { await bookings.ApproveCancelAsync(id, ct); return NoContent(); }

    private void EnsureAccess(Bookings b) { if (!current.Roles.Any(x => RoleCodes.InternalStaff.Contains(x)) && current.CustomerId != b.CustomerId) throw new ForbiddenException("Booking does not belong to this customer."); }
}
