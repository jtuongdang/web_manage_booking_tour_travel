using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.BookingEngine;
using Xunit;

namespace QuanLyTourDuLich.Tests.BookingEngineTests;

public sealed class BookingPricingTests
{
    private static TourDepartures Departure() => new()
    {
        AdultPrice = 1_000m, ChildPrice = 750m, InfantPrice = 100m, SingleSupplement = 200m
    };

    [Fact]
    public void CalculatesIncludedTaxAfterDiscountAndDeposit()
    {
        var result = BookingPricing.Calculate(Departure(), 2, 1, 1, 1, 300m, 10m, "Included", 30m);

        Assert.Equal(3_050m, result.SubTotal);
        Assert.Equal(300m, result.Discount);
        Assert.Equal(2_500m, result.BeforeTax);
        Assert.Equal(250m, result.Vat);
        Assert.Equal(2_750m, result.Total);
        Assert.Equal(825m, result.Deposit);
    }

    [Fact]
    public void CalculatesExcludedTaxAndCapsDiscountAtSubtotal()
    {
        var result = BookingPricing.Calculate(Departure(), 1, 0, 0, 0, 2_000m, 8m, "Excluded");

        Assert.Equal(1_000m, result.SubTotal);
        Assert.Equal(1_000m, result.Discount);
        Assert.Equal(0m, result.Total);
        Assert.Equal(0m, result.Vat);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    public void RejectsInvalidPassengerCounts(int adults, int children, int infants)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BookingPricing.Calculate(Departure(), adults, children, infants, 0, 0m, 10m, "Included"));
    }

    [Fact]
    public void CapacityRulesAllowLastSeatButRejectConcurrentOverbooking()
    {
        Assert.True(BookingStateRules.CanReserve(10, 3, 6, 1, 1));
        Assert.False(BookingStateRules.CanReserve(10, 3, 6, 1, 2));
        Assert.False(BookingStateRules.CanReserve(10, 4, 6, 0, 1));
    }

    [Theory]
    [InlineData("PendingPayment", true)]
    [InlineData("DepositPaid", true)]
    [InlineData("Confirmed", true)]
    [InlineData("Expired", false)]
    [InlineData("Cancelled", false)]
    public void StateRulesLimitEditsAndCancellationRequests(string status, bool allowed)
    {
        Assert.Equal(allowed, BookingStateRules.CanEdit(status));
        Assert.Equal(allowed, BookingStateRules.CanRequestCancellation(status));
    }
}
