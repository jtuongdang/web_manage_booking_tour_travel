using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Tours;

[ApiController]
[Route("api/v1/tours")]
public sealed class ToursController(AppDbContext db, TourCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] TourQuery query, CancellationToken ct)
    {
        var staff = User.Identity?.IsAuthenticated == true && RoleCodes.InternalStaff.Any(User.IsInRole);
        var (items, total) = await catalog.SearchAsync(query, staff, ct);
        return Ok(new { items, pageIndex = query.PageIndex, pageSize = query.PageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)query.PageSize) });
    }

    [HttpGet("{slugOrId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlugOrId(string slugOrId, CancellationToken ct)
    {
        var staff = User.Identity?.IsAuthenticated == true && RoleCodes.InternalStaff.Any(User.IsInRole);
        return Ok(await catalog.GetAsync(slugOrId, staff, ct));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Create(TourWriteRequest request, CancellationToken ct)
    {
        Validate(request);
        if (!await db.CancellationPolicies.AnyAsync(p => p.Id == request.CancellationPolicyId && p.IsActive, ct)) throw new BadRequestException("An active cancellation policy is required.");
        if (await db.Tours.AnyAsync(t => t.Code == request.Code || t.Slug == request.Slug, ct)) throw new ConflictException("Tour code or slug already exists.");
        var tour = new QuanLyTourDuLich.Api.Models.Entities.Tours { Code = request.Code, Slug = request.Slug, Title = request.Title, TourType = request.TourType,
            ScopeType = request.ScopeType, Tags = request.Tags, DepartureFrom = request.DepartureFrom, Destination = request.Destination,
            DurationDays = request.DurationDays, DurationNights = request.DurationNights, Transport = request.Transport,
            OriginalPrice = request.OriginalPrice, BaseAdultPrice = request.BaseAdultPrice, ChildPricePercent = request.ChildPricePercent,
            InfantPrice = request.InfantPrice, SingleSupplement = request.SingleSupplement, CancellationPolicyId = request.CancellationPolicyId,
            Summary = request.Summary, HighlightsJson = request.HighlightsJson, IncludesJson = request.IncludesJson, ExcludesJson = request.ExcludesJson,
            Badges = request.Badges, IsPublic = request.IsPublic, Status = "Draft", CreatedByUserId = GetUserId(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Tours.Add(tour);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBySlugOrId), new { slugOrId = tour.Slug }, tour);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Update(int id, TourWriteRequest request, CancellationToken ct)
    {
        Validate(request);
        var tour = await db.Tours.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Tour was not found.");
        if (!await db.CancellationPolicies.AnyAsync(p => p.Id == request.CancellationPolicyId && p.IsActive, ct)) throw new BadRequestException("An active cancellation policy is required.");
        if (await db.Tours.AnyAsync(t => t.Id != id && (t.Code == request.Code || t.Slug == request.Slug), ct)) throw new ConflictException("Tour code or slug already exists.");
        tour.Code = request.Code; tour.Slug = request.Slug; tour.Title = request.Title; tour.TourType = request.TourType; tour.ScopeType = request.ScopeType;
        tour.Tags = request.Tags; tour.DepartureFrom = request.DepartureFrom; tour.Destination = request.Destination; tour.DurationDays = request.DurationDays;
        tour.DurationNights = request.DurationNights; tour.Transport = request.Transport; tour.OriginalPrice = request.OriginalPrice;
        tour.BaseAdultPrice = request.BaseAdultPrice; tour.ChildPricePercent = request.ChildPricePercent; tour.InfantPrice = request.InfantPrice;
        tour.SingleSupplement = request.SingleSupplement; tour.CancellationPolicyId = request.CancellationPolicyId; tour.Summary = request.Summary;
        tour.HighlightsJson = request.HighlightsJson; tour.IncludesJson = request.IncludesJson; tour.ExcludesJson = request.ExcludesJson;
        tour.Badges = request.Badges; tour.IsPublic = request.IsPublic; tour.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(tour);
    }

    [HttpPut("{id:int}/publish")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        var tour = await db.Tours.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Tour was not found.");
        tour.Status = "Published"; tour.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(tour);
    }

    [HttpPut("{id:int}/itineraries")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> ReplaceItineraries(int id, ItineraryWriteRequest request, CancellationToken ct)
    {
        if (!await db.Tours.AnyAsync(x => x.Id == id, ct)) throw new NotFoundException("Tour was not found.");
        if (request.Items.Select(x => x.DayNumber).Distinct().Count() != request.Items.Count) throw new BadRequestException("Each itinerary day number must be unique.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var old = await db.TourItineraries.Where(x => x.TourId == id).ToListAsync(ct); db.TourItineraries.RemoveRange(old);
        db.TourItineraries.AddRange(request.Items.Select(x => new TourItineraries { TourId = id, DayNumber = x.DayNumber, Title = x.Title, Meals = x.Meals, Accommodation = x.Accommodation, Description = x.Description }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:int}/images")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> ReplaceImages(int id, ImageWriteRequest request, CancellationToken ct)
    {
        if (!await db.Tours.AnyAsync(x => x.Id == id, ct)) throw new NotFoundException("Tour was not found.");
        if (request.Items.Count(x => x.IsCover) > 1) throw new BadRequestException("At most one cover image is allowed.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var old = await db.TourImages.Where(x => x.TourId == id).ToListAsync(ct); db.TourImages.RemoveRange(old);
        db.TourImages.AddRange(request.Items.Select(x => new TourImages { TourId = id, ImageUrl = x.ImageUrl, AltText = x.AltText, IsCover = x.IsCover, SortOrder = x.SortOrder }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return NoContent();
    }

    private int? GetUserId() => int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    private static void Validate(TourWriteRequest r)
    {
        if (!TourValidation.IsKnownTourType(r.TourType)) throw new BadRequestException("TourType must be GroupTour, PrivateTour, or CustomTour.");
        if (!TourValidation.IsKnownScope(r.ScopeType)) throw new BadRequestException("ScopeType must be Domestic or International.");
        if (!TourValidation.IsValidJson(r.HighlightsJson) || !TourValidation.IsValidJson(r.IncludesJson) || !TourValidation.IsValidJson(r.ExcludesJson)) throw new BadRequestException("Tour list fields must contain valid JSON.");
    }
}
