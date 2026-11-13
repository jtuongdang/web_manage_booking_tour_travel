using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using PromotionEntity = QuanLyTourDuLich.Api.Models.Entities.Promotions;

namespace QuanLyTourDuLich.Api.Modules.Promotions;

public interface IPromotionValidationService
{
    Task<PromotionValidationResult> ValidateAsync(ValidatePromotionRequest request, CancellationToken cancellationToken = default);
}

public sealed record ValidatePromotionRequest(
    string Code,
    int TourId,
    int? CustomerId,
    decimal OrderAmount,
    int PaxCount,
    DateTime? At = null);

public sealed record PromotionValidationResult(
    bool IsValid,
    string Code,
    string? Reason,
    decimal DiscountAmount,
    decimal PayableAmount,
    int? PromotionId = null,
    string? DiscountType = null);

public sealed class PromotionValidationService(AppDbContext db) : IPromotionValidationService
{
    public async Task<PromotionValidationResult> ValidateAsync(ValidatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        var promotion = await db.Promotions.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == code, cancellationToken);
        if (promotion is null)
            return Invalid(code, request.OrderAmount, "Promotion code was not found.");

        var appliesToTour = promotion.ApplyAllTours || await db.PromotionTours.AsNoTracking()
            .AnyAsync(pt => pt.PromotionId == promotion.Id && pt.TourId == request.TourId, cancellationToken);
        var usageCount = request.CustomerId is > 0
            ? await db.PromotionUsages.AsNoTracking()
                .CountAsync(u => u.PromotionId == promotion.Id && u.CustomerId == request.CustomerId.Value &&
                    (u.Status == "Reserved" || u.Status == "Redeemed"), cancellationToken)
            : 0;

        return Evaluate(promotion, request, DateTime.UtcNow, appliesToTour, usageCount);
    }

    public static PromotionValidationResult Evaluate(
        PromotionEntity promotion,
        ValidatePromotionRequest request,
        DateTime now,
        bool appliesToTour,
        int customerUsageCount)
    {
        var code = request.Code.Trim();
        if (!promotion.IsActive)
            return Invalid(code, request.OrderAmount, "Promotion is inactive.", promotion.Id);
        var at = request.At ?? now;
        if (at < promotion.ValidFrom || at > promotion.ValidTo)
            return Invalid(code, request.OrderAmount, "Promotion is outside its validity period.", promotion.Id);
        if (request.OrderAmount < 0 || request.PaxCount < 1)
            return Invalid(code, request.OrderAmount, "Order amount or passenger count is invalid.", promotion.Id);
        if (request.OrderAmount < promotion.MinOrderAmount)
            return Invalid(code, request.OrderAmount, "Minimum order amount was not met.", promotion.Id);
        if (request.PaxCount < promotion.MinPax)
            return Invalid(code, request.OrderAmount, "Minimum passenger count was not met.", promotion.Id);
        if (!appliesToTour)
            return Invalid(code, request.OrderAmount, "Promotion does not apply to this tour.", promotion.Id);
        if (promotion.UsageLimit is > 0 && promotion.UsedCount >= promotion.UsageLimit.Value)
            return Invalid(code, request.OrderAmount, "Promotion usage limit has been reached.", promotion.Id);
        if (promotion.MaxUsagePerCustomer > 0 && customerUsageCount >= promotion.MaxUsagePerCustomer)
            return Invalid(code, request.OrderAmount, "Customer usage limit has been reached.", promotion.Id);

        var discount = promotion.DiscountType switch
        {
            "Percent" => request.OrderAmount * promotion.DiscountValue / 100m,
            "FixedBooking" => promotion.DiscountValue,
            "FixedPerPax" => promotion.DiscountValue * request.PaxCount,
            _ => 0m
        };
        if (promotion.DiscountType == "Percent" && promotion.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
        discount = Math.Round(Math.Min(Math.Max(discount, 0m), request.OrderAmount), 2, MidpointRounding.AwayFromZero);
        return new PromotionValidationResult(true, promotion.Code, null, discount,
            request.OrderAmount - discount, promotion.Id, promotion.DiscountType);
    }

    private static PromotionValidationResult Invalid(string code, decimal amount, string reason, int? id = null) =>
        new(false, code, reason, 0m, Math.Max(amount, 0m), id);
}
