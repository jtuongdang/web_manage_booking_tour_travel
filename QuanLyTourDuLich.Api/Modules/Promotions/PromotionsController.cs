using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using PromotionEntity = QuanLyTourDuLich.Api.Models.Entities.Promotions;

namespace QuanLyTourDuLich.Api.Modules.Promotions;

[ApiController]
[Route("api/v1/promotions")]
public sealed class PromotionsController(AppDbContext db, IPromotionValidationService validation) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PromotionListItem>>> ListPublic(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = await db.Promotions.AsNoTracking()
            .Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now &&
                (p.UsageLimit == null || p.UsedCount < p.UsageLimit))
            .OrderBy(p => p.ValidTo)
            .Select(p => new PromotionListItem(p.Id, p.Code, p.Name, p.Description,
                p.DiscountType, p.DiscountValue, p.MaxDiscountAmount, p.MinOrderAmount, p.MinPax,
                p.ApplyAllTours, p.UsageLimit, p.UsedCount, p.MaxUsagePerCustomer, p.ValidFrom, p.ValidTo, p.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("admin")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<PagedResult<PromotionListItem>>> ListAdmin([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.Promotions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(p => p.Code.Contains(search) || p.Name.Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip((request.NormalizedPageIndex - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize).Select(p => new PromotionListItem(p.Id, p.Code, p.Name, p.Description,
                p.DiscountType, p.DiscountValue, p.MaxDiscountAmount, p.MinOrderAmount, p.MinPax,
                p.ApplyAllTours, p.UsageLimit, p.UsedCount, p.MaxUsagePerCustomer, p.ValidFrom, p.ValidTo, p.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResult<PromotionListItem>(items, request.NormalizedPageIndex, request.NormalizedPageSize, count));
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionValidationResult>> Validate(ValidatePromotionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) throw new BadRequestException("Promotion code is required.");
        return Ok(await validation.ValidateAsync(request, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<PromotionListItem>> Create(SavePromotionRequest request, CancellationToken cancellationToken)
    {
        var values = NormalizeAndValidate(request);
        if (await db.Promotions.AnyAsync(p => p.Code == values.Code, cancellationToken))
            throw new ConflictException("A promotion with this code already exists.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var promotion = new PromotionEntity
        {
            Code = values.Code, Name = values.Name, Description = values.Description,
            DiscountType = values.DiscountType, DiscountValue = values.DiscountValue,
            MaxDiscountAmount = values.MaxDiscountAmount, MinOrderAmount = values.MinOrderAmount,
            MinPax = values.MinPax, ApplyAllTours = values.ApplyAllTours,
            UsageLimit = values.UsageLimit, UsedCount = 0, MaxUsagePerCustomer = values.MaxUsagePerCustomer,
            ValidFrom = values.ValidFrom, ValidTo = values.ValidTo, IsActive = values.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.Promotions.Add(promotion);
        await db.SaveChangesAsync(cancellationToken);
        if (!promotion.ApplyAllTours)
            db.PromotionTours.AddRange(values.TourIds.Distinct().Select(tourId => new PromotionTours { PromotionId = promotion.Id, TourId = tourId }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(ListAdmin), new { id = promotion.Id }, ToListItem(promotion));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<PromotionListItem>> Update(int id, SavePromotionRequest request, CancellationToken cancellationToken)
    {
        var values = NormalizeAndValidate(request);
        var promotion = await db.Promotions.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("Promotion not found.");
        if (await db.Promotions.AnyAsync(p => p.Id != id && p.Code == values.Code, cancellationToken))
            throw new ConflictException("A promotion with this code already exists.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        promotion.Code = values.Code; promotion.Name = values.Name; promotion.Description = values.Description;
        promotion.DiscountType = values.DiscountType; promotion.DiscountValue = values.DiscountValue;
        promotion.MaxDiscountAmount = values.MaxDiscountAmount; promotion.MinOrderAmount = values.MinOrderAmount;
        promotion.MinPax = values.MinPax; promotion.ApplyAllTours = values.ApplyAllTours;
        promotion.UsageLimit = values.UsageLimit; promotion.MaxUsagePerCustomer = values.MaxUsagePerCustomer;
        promotion.ValidFrom = values.ValidFrom; promotion.ValidTo = values.ValidTo; promotion.IsActive = values.IsActive;
        var oldTours = await db.PromotionTours.Where(pt => pt.PromotionId == id).ToListAsync(cancellationToken);
        db.PromotionTours.RemoveRange(oldTours);
        await db.SaveChangesAsync(cancellationToken);
        if (!promotion.ApplyAllTours)
            db.PromotionTours.AddRange(values.TourIds.Distinct().Select(tourId => new PromotionTours { PromotionId = id, TourId = tourId }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(ToListItem(promotion));
    }

    private static PromotionValues NormalizeAndValidate(SavePromotionRequest r)
    {
        var code = r.Code.Trim().ToUpperInvariant();
        var name = r.Name.Trim();
        var type = r.DiscountType.Trim();
        if (code.Length is < 1 or > 40 || name.Length is < 1 or > 150 || r.Description?.Length > 400 ||
            type is not ("Percent" or "FixedBooking" or "FixedPerPax") || r.DiscountValue <= 0 ||
            r.MaxDiscountAmount < 0 || r.MinOrderAmount < 0 || r.MinPax < 1 || r.UsageLimit < 1 ||
            r.MaxUsagePerCustomer < 1 || r.ValidTo <= r.ValidFrom ||
            (!r.ApplyAllTours && (r.TourIds is null || r.TourIds.Count == 0)))
            throw new BadRequestException("Invalid promotion values.");
        return new PromotionValues(code, name, string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim(),
            type, r.DiscountValue, r.MaxDiscountAmount, r.MinOrderAmount, r.MinPax, r.ApplyAllTours,
            r.UsageLimit, r.MaxUsagePerCustomer, r.ValidFrom, r.ValidTo, r.IsActive, r.TourIds ?? []);
    }

    private static PromotionListItem ToListItem(PromotionEntity p) => new(p.Id, p.Code, p.Name, p.Description,
        p.DiscountType, p.DiscountValue, p.MaxDiscountAmount, p.MinOrderAmount, p.MinPax,
        p.ApplyAllTours, p.UsageLimit, p.UsedCount, p.MaxUsagePerCustomer, p.ValidFrom, p.ValidTo, p.IsActive);

    private sealed record PromotionValues(string Code, string Name, string? Description, string DiscountType,
        decimal DiscountValue, decimal? MaxDiscountAmount, decimal MinOrderAmount, int MinPax,
        bool ApplyAllTours, int? UsageLimit, int MaxUsagePerCustomer, DateTime ValidFrom, DateTime ValidTo,
        bool IsActive, IReadOnlyList<int> TourIds);
}

public sealed record SavePromotionRequest(
    string Code, string Name, string? Description, string DiscountType, decimal DiscountValue,
    decimal? MaxDiscountAmount, decimal MinOrderAmount, int MinPax, bool ApplyAllTours,
    int? UsageLimit, int MaxUsagePerCustomer, DateTime ValidFrom, DateTime ValidTo,
    bool IsActive = true, IReadOnlyList<int>? TourIds = null);

public sealed record PromotionListItem(
    int Id, string Code, string Name, string? Description, string DiscountType, decimal DiscountValue,
    decimal? MaxDiscountAmount, decimal MinOrderAmount, int MinPax, bool ApplyAllTours,
    int? UsageLimit, int UsedCount, int MaxUsagePerCustomer, DateTime ValidFrom, DateTime ValidTo, bool IsActive);
