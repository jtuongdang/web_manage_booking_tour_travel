using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.Promotions;
using Xunit;
using PromotionEntity = QuanLyTourDuLich.Api.Models.Entities.Promotions;

namespace QuanLyTourDuLich.Tests.Promotions;

public sealed class PromotionValidationTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Welcome10_AppliesPercentAndMaximumDiscount()
    {
        var result = Validate(Promotion("WELCOME10", "Percent", 10m, maxDiscount: 500_000m), 8_000_000m, 2);

        Assert.True(result.IsValid);
        Assert.Equal(500_000m, result.DiscountAmount);
        Assert.Equal(7_500_000m, result.PayableAmount);
    }

    [Fact]
    public void Group300_AppliesFixedBookingOnlyWhenGroupThresholdMet()
    {
        var promotion = Promotion("GROUP300", "FixedBooking", 300_000m, minPax: 4);

        var valid = Validate(promotion, 2_000_000m, 4);
        var tooSmall = Validate(promotion, 2_000_000m, 3);

        Assert.True(valid.IsValid);
        Assert.Equal(300_000m, valid.DiscountAmount);
        Assert.Equal(1_700_000m, valid.PayableAmount);
        Assert.False(tooSmall.IsValid);
    }

    [Fact]
    public void FixedPerPax_MultipliesByPassengerCountAndDoesNotExceedOrderAmount()
    {
        var promotion = Promotion("PAX100", "FixedPerPax", 100_000m);

        var result = Validate(promotion, 250_000m, 3);

        Assert.Equal(250_000m, result.DiscountAmount);
        Assert.Equal(0m, result.PayableAmount);
    }

    [Fact]
    public void RejectsExhaustedGlobalAndCustomerQuotas()
    {
        var globalLimit = Promotion("LIMIT", "FixedBooking", 100m, usageLimit: 5, usedCount: 5);
        var customerLimit = Promotion("CUSTOMERLIMIT", "FixedBooking", 100m, maxPerCustomer: 1);

        Assert.False(Validate(globalLimit, 1_000m, 1).IsValid);
        Assert.False(Validate(customerLimit, 1_000m, 1, usageCount: 1).IsValid);
    }

    [Fact]
    public void IgnoresDefaultZeroPerCustomerLimitAndRejectsExpiredPromotion()
    {
        var noConfiguredLimit = Promotion("NO_LIMIT", "FixedBooking", 100m, maxPerCustomer: 0);
        var expired = Promotion("EXPIRED", "FixedBooking", 100m, validTo: Now.AddSeconds(-1));

        Assert.True(Validate(noConfiguredLimit, 1_000m, 1, usageCount: 100).IsValid);
        var expiredResult = Validate(expired, 1_000m, 1);
        Assert.False(expiredResult.IsValid);
        Assert.Contains("validity", expiredResult.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static PromotionValidationResult Validate(PromotionEntity promotion, decimal amount, int pax, int usageCount = 0) =>
        PromotionValidationService.Evaluate(
            promotion,
            new ValidatePromotionRequest(promotion.Code, TourId: 4, CustomerId: 9, OrderAmount: amount, PaxCount: pax),
            Now,
            appliesToTour: true,
            customerUsageCount: usageCount);

    private static PromotionEntity Promotion(
        string code,
        string type,
        decimal value,
        decimal? maxDiscount = null,
        int minPax = 1,
        int? usageLimit = null,
        int usedCount = 0,
        int maxPerCustomer = 1,
        DateTime? validTo = null) => new()
    {
        Id = 2,
        Code = code,
        Name = code,
        DiscountType = type,
        DiscountValue = value,
        MaxDiscountAmount = maxDiscount,
        MinOrderAmount = 0,
        MinPax = minPax,
        ApplyAllTours = true,
        UsageLimit = usageLimit,
        UsedCount = usedCount,
        MaxUsagePerCustomer = maxPerCustomer,
        ValidFrom = Now.AddDays(-1),
        ValidTo = validTo ?? Now.AddDays(1),
        IsActive = true
    };
}
