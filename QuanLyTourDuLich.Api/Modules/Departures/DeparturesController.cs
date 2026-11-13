using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Departures;

[ApiController]
[Route("api/v1/departures")]
public sealed class DeparturesController(AppDbContext db, DepartureService service) : ControllerBase
{
    [HttpGet("/api/v1/tours/{tourId:int}/departures")]
    [AllowAnonymous]
    public async Task<IActionResult> ForTour(int tourId, [FromQuery] bool includeInternal = false, CancellationToken ct = default)
    {
        var internalView = includeInternal && IsInternal();
        var visibleTour = await db.Tours.AsNoTracking().AnyAsync(t => t.Id == tourId && (internalView || (t.IsPublic && t.Status == "Published")), ct);
        if (!visibleTour) throw new NotFoundException("Tour was not found.");
        var query = db.TourDepartures.AsNoTracking().Where(d => d.TourId == tourId);
        if (!internalView) query = query.Where(d => d.IsPublic && d.Status != "Cancelled");
        return Ok(await query.OrderBy(d => d.DepartureDate).ToListAsync(ct));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInternal = false, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        ValidatePage(pageIndex, pageSize);
        var internalView = includeInternal && IsInternal();
        var query = db.TourDepartures.AsNoTracking().AsQueryable();
        if (!internalView) query = query.Where(d => d.IsPublic && d.Status != "Cancelled" && db.Tours.Any(t => t.Id == d.TourId && t.IsPublic && t.Status == "Published"));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(d => d.DepartureDate).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { items, pageIndex, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await service.GetAsync(id, IsInternal(), ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Create(DepartureWriteRequest request, CancellationToken ct)
    {
        await service.EnsureReferencesAsync(request, ct);
        if (await db.TourDepartures.AnyAsync(d => d.DepartureCode == request.DepartureCode, ct)) throw new ConflictException("Departure code already exists.");
        var departure = new TourDepartures { TourId = request.TourId, DepartureCode = request.DepartureCode,
            DepartureDate = request.DepartureDate, ReturnDate = request.ReturnDate, CutoffAt = request.CutoffAt,
            AdultPrice = request.AdultPrice, ChildPrice = request.ChildPrice, InfantPrice = request.InfantPrice, SingleSupplement = request.SingleSupplement,
            TotalCapacity = request.TotalCapacity, HeldSlots = 0, BookedSlots = 0, MinParticipants = request.MinParticipants, MaxInfants = request.MaxInfants,
            CancellationPolicyId = request.CancellationPolicyId, IsPublic = request.IsPublic, ContractCode = request.ContractCode,
            Status = request.TotalCapacity <= 0 ? "Full" : "Open", VehicleType = request.VehicleType, VehiclePlate = request.VehiclePlate,
            DriverName = request.DriverName, DriverPhone = request.DriverPhone, Notes = request.Notes, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TourDepartures.Add(departure); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = departure.Id }, departure);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Update(int id, DepartureWriteRequest request, CancellationToken ct)
    {
        var departure = await db.TourDepartures.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new NotFoundException("Departure was not found.");
        if (departure.Status == "Settled") throw new ConflictException("Settled departures are locked.");
        await service.EnsureReferencesAsync(request, ct);
        if (departure.TourId != request.TourId && (departure.HeldSlots != 0 || departure.BookedSlots != 0)) throw new ConflictException("A departure with reservations cannot be moved to another tour.");
        if (await db.TourDepartures.AnyAsync(d => d.Id != id && d.DepartureCode == request.DepartureCode, ct)) throw new ConflictException("Departure code already exists.");
        if (!DepartureValidation.IsValidCapacity(request.TotalCapacity, departure.HeldSlots, departure.BookedSlots)) throw new ConflictException("Capacity cannot be less than held and booked seats.");
        departure.TourId = request.TourId; departure.DepartureCode = request.DepartureCode; departure.DepartureDate = request.DepartureDate;
        departure.ReturnDate = request.ReturnDate; departure.CutoffAt = request.CutoffAt; departure.AdultPrice = request.AdultPrice;
        departure.ChildPrice = request.ChildPrice; departure.InfantPrice = request.InfantPrice; departure.SingleSupplement = request.SingleSupplement;
        departure.TotalCapacity = request.TotalCapacity; departure.MinParticipants = request.MinParticipants; departure.MaxInfants = request.MaxInfants;
        departure.CancellationPolicyId = request.CancellationPolicyId; departure.IsPublic = request.IsPublic; departure.ContractCode = request.ContractCode;
        departure.VehicleType = request.VehicleType; departure.VehiclePlate = request.VehiclePlate; departure.DriverName = request.DriverName;
        departure.DriverPhone = request.DriverPhone; departure.Notes = request.Notes; departure.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Departure was changed concurrently; reload and retry."); }
        return Ok(departure);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> SetStatus(int id, DepartureStatusRequest request, CancellationToken ct)
    {
        if (!DepartureValidation.Statuses.Contains(request.Status, StringComparer.Ordinal)) throw new BadRequestException("Departure status is invalid.");
        var departure = await db.TourDepartures.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new NotFoundException("Departure was not found.");
        if (departure.Status == "Settled" && request.Status != "Settled") throw new ConflictException("Settled departures are locked.");
        if (request.Status == "Settled" && !User.IsInRole(RoleCodes.Admin) && !User.IsInRole(RoleCodes.Accountant)) throw new ForbiddenException("Only an accountant or administrator may mark a departure as settled.");
        if (request.Status != "Settled" && !User.IsInRole(RoleCodes.Operator) && !User.IsInRole(RoleCodes.Admin)) throw new ForbiddenException("Only an operator or administrator may change operational departure status.");
        var availableSlots = departure.TotalCapacity - departure.HeldSlots - departure.BookedSlots;
        if (request.Status == "Full" && availableSlots > 0) throw new BadRequestException("A departure with available seats cannot be marked Full.");
        if (request.Status == "Open" && availableSlots <= 0) throw new ConflictException("A full departure cannot be reopened until seats are available.");
        departure.Status = request.Status; departure.UpdatedAt = DateTime.UtcNow;
        if (request.Status == "Settled") { departure.SettledAt = DateTime.UtcNow; departure.SettledByUserId = ParseUserId(); }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Departure was changed concurrently; reload and retry."); }
        return Ok(departure);
    }

    [HttpGet("{id:int}/manifest")]
    [Authorize(Policy = PolicyNames.RequireInternalStaff)]
    public async Task<IActionResult> Manifest(int id, CancellationToken ct)
    {
        if (!await db.TourDepartures.AnyAsync(d => d.Id == id, ct)) throw new NotFoundException("Departure was not found.");
        var privileged = User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.Operator) || User.IsInRole(RoleCodes.Sales);
        var rows = await (from b in db.Bookings.AsNoTracking()
                          join p in db.BookingPassengers.AsNoTracking() on b.Id equals p.BookingId
                          where b.DepartureId == id && (b.Status == "PendingPayment" || b.Status == "DepositPaid" || b.Status == "Confirmed" || b.Status == "FinalPaymentOverdue")
                          orderby b.BookingCode, p.Id
                          select new { PassengerId = p.Id, BookingId = b.Id, b.BookingCode, b.Status, p.PaxType, p.FullName, p.IdentityNumber, p.Nationality, p.CheckInStatus }).ToListAsync(ct);
        return Ok(rows.Select(p => new ManifestPassenger(p.PassengerId, p.BookingId, p.BookingCode, p.Status, p.PaxType, p.FullName,
            privileged ? p.IdentityNumber : DepartureValidation.MaskIdentity(p.IdentityNumber), p.Nationality, p.CheckInStatus)));
    }

    private bool IsInternal() => User.Identity?.IsAuthenticated == true && RoleCodes.InternalStaff.Any(User.IsInRole);
    private int? ParseUserId() => int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    private static void ValidatePage(int page, int size) { if (page < 1 || size is < 1 or > 100) throw new BadRequestException("PageIndex must be positive and PageSize must be between 1 and 100."); }
}
