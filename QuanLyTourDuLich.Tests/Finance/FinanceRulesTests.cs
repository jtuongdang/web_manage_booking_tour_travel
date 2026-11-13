using QuanLyTourDuLich.Api.Modules.Finance;
using QuanLyTourDuLich.Api.Models.Entities;
using Xunit;

namespace QuanLyTourDuLich.Tests.Finance;

public sealed class FinanceRulesTests
{
    [Theory]
    [InlineData(1000, 100, 900)]
    [InlineData(100, 150, 0)]
    [InlineData(0, 0, 0)]
    public void Refundable_amount_never_goes_below_zero(decimal paid, decimal penalty, decimal expected)
    {
        Assert.Equal(expected, FinanceRules.CalculateRefundAmount(paid, penalty));
    }

    [Fact]
    public void Company_cancellation_has_no_penalty()
    {
        var booking = new Bookings { TotalAmount = 2_000_000m, CancelPenaltyPercent = 50m };

        Assert.Equal(0m, FinanceRules.CalculatePenalty(booking, 75m, companyCancelled: true));
    }

    [Fact]
    public void Uses_frozen_booking_cancellation_percent()
    {
        var booking = new Bookings { TotalAmount = 1_250_000m, CancelPenaltyPercent = 20m };

        Assert.Equal(250_000m, FinanceRules.CalculatePenalty(booking, 80m, companyCancelled: false));
    }

    [Fact]
    public void Reads_matching_percent_from_policy_snapshot()
    {
        var booking = new Bookings
        {
            TotalAmount = 1_250_000m,
            CancellationPolicySnapshotJson = """{"rules":[{"minDaysBeforeDeparture":0,"maxDaysBeforeDeparture":3,"penaltyPercent":80},{"minDaysBeforeDeparture":4,"maxDaysBeforeDeparture":30,"penaltyPercent":20}]}"""
        };

        var penalty = FinanceRules.CalculatePenaltyFromSnapshot(booking, new DateOnly(2026, 12, 20), new DateTime(2026, 12, 10));

        Assert.Equal(250_000m, penalty);
    }
}
