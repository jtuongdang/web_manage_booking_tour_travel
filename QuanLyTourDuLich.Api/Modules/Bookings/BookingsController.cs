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
    public async Task<ActionResult<BookingDetailsDto>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var isWebChannel = string.IsNullOrWhiteSpace(request.BookingChannel)
            || string.Equals(request.BookingChannel, "WebB2C", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.BookingChannel, "Web", StringComparison.OrdinalIgnoreCase);
        if (current.IsAuthenticated && !current.Roles.Any(r => r is RoleCodes.Sales or RoleCodes.Admin or RoleCodes.Operator) && !isWebChannel)
            throw new ForbiddenException("Only staff may create an assisted booking.");
        var result = await bookings.CreateAsync(request, ct);
        var details = await bookings.BuildDetailsDtoAsync(result, ct);
        return CreatedAtAction(nameof(Get), new { codeOrId = result.BookingCode }, details);
    }

    [HttpGet("my"), Authorize(Roles = RoleCodes.Customer)]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> Mine(CancellationToken ct)
    {
        var items = await BuildListQuery()
            .Where(x => x.Booking.CustomerId == current.CustomerId)
            .OrderByDescending(x => x.Booking.CreatedAt)
            .Select(x => ProjectListItem(x.Booking, x.TourTitle, x.TourCode, x.DepartureCode, x.DepartureDate))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet, Authorize(Roles = RoleCodes.Sales + "," + RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<ActionResult<PagedResult<BookingListItemDto>>> List([FromQuery] PagedRequest page, [FromQuery] string? status, CancellationToken ct)
    {
        var query = BuildListQuery();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Booking.Status == status);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var s = page.Search.Trim();
            query = query.Where(x => x.Booking.BookingCode.Contains(s) || x.Booking.ContactName.Contains(s) || x.Booking.ContactPhone.Contains(s) || x.Booking.ContactEmail.Contains(s));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Booking.CreatedAt)
            .Skip((page.NormalizedPageIndex - 1) * page.NormalizedPageSize)
            .Take(page.NormalizedPageSize)
            .Select(x => ProjectListItem(x.Booking, x.TourTitle, x.TourCode, x.DepartureCode, x.DepartureDate))
            .ToListAsync(ct);
        return Ok(new PagedResult<BookingListItemDto>(items, page.NormalizedPageIndex, page.NormalizedPageSize, count));
    }

    [HttpGet("{codeOrId}")]
    public async Task<ActionResult<BookingDetailsDto>> Get(string codeOrId, [FromQuery] string? phone, [FromQuery] string? email, CancellationToken ct)
    {
        var query = db.Bookings.AsNoTracking().AsQueryable();
        if (long.TryParse(codeOrId, out var id)) query = query.Where(x => x.Id == id); else query = query.Where(x => x.BookingCode == codeOrId);
        var item = await query.SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Booking not found.");
        var staff = current.Roles.Any(x => RoleCodes.InternalStaff.Contains(x));
        if (!staff && current.CustomerId != item.CustomerId && !(phone == item.ContactPhone && !string.IsNullOrWhiteSpace(phone)) && !(email != null && string.Equals(email, item.ContactEmail, StringComparison.OrdinalIgnoreCase)))
            throw new ForbiddenException("Provide the verified contact phone/email or sign in as the booking owner.");
        return Ok(await bookings.BuildDetailsDtoAsync(item, ct));
    }

    [HttpPut("{id:long}/passengers"), Authorize]
    public async Task<IActionResult> UpdatePassengers(long id, IReadOnlyList<PassengerRequest> passengers, CancellationToken ct)
    {
        var booking = await bookings.GetAsync(id, ct); EnsureAccess(booking);
        var departure = await db.TourDepartures.SingleAsync(x => x.Id == booking.DepartureId, ct);
        if (departure.CutoffAt < DateTime.UtcNow) throw new ConflictException("Departure cutoff has passed.");
        BookingService.ValidateManifest(passengers, booking.AdultCount, booking.ChildCount, booking.InfantCount);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var old = await db.BookingPassengers.Where(x => x.BookingId == id).ToListAsync(ct); db.BookingPassengers.RemoveRange(old);
        var now = DateTime.UtcNow;
        db.BookingPassengers.AddRange(BookingService.ToPassengerEntities(id, passengers, now));
        await db.SaveChangesAsync(ct); await audit.WriteAsync("UpdatePassengers", "Bookings", id.ToString(), old.Count, passengers.Count, current.UserId, cancellationToken: ct); await tx.CommitAsync(ct); return NoContent();
    }

    private sealed record BookingProjection(Bookings Booking, string TourTitle, string TourCode, string DepartureCode, DateOnly DepartureDate);

    private IQueryable<BookingProjection> BuildListQuery() =>
        from b in db.Bookings.AsNoTracking()
        join d in db.TourDepartures.AsNoTracking() on b.DepartureId equals d.Id
        join t in db.Tours.AsNoTracking() on b.TourId equals t.Id
        select new BookingProjection(b, t.Title, t.Code, d.DepartureCode, d.DepartureDate);

    private static BookingListItemDto ProjectListItem(Bookings b, string tourTitle, string tourCode, string departureCode, DateOnly departureDate) =>
        new(b.Id, b.BookingCode, b.TourId, b.DepartureId, b.CustomerId, b.CreatedByUserId, b.SalesUserId, b.BookingChannel,
            b.ContactName, b.ContactPhone, b.ContactEmail, b.SpecialNote, b.AdultCount, b.ChildCount, b.InfantCount,
            b.SingleRoomCount, BookingStateRules.SeatsCount(b.AdultCount, b.ChildCount), b.SubTotal, b.DiscountAmount,
            b.TaxMode, b.VatRate, b.AmountBeforeTax, b.VatAmount, b.TotalAmount, b.RequiredDepositAmount, b.PaidAmount,
            b.CreditAmount, Math.Max(0m, b.TotalAmount - b.PaidAmount), b.PromotionId, b.PromoCodeApplied, b.PayOption,
            b.RequireVatInvoice, b.Status, b.HoldExpiresAt, b.FinalPaymentDueAt, b.CreatedAt, b.UpdatedAt,
            tourTitle, tourCode, departureCode, departureDate);

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
