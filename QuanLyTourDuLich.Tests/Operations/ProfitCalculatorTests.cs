using QuanLyTourDuLich.Api.Modules.Reports;
using QuanLyTourDuLich.Api.Modules.Operations;
using QuanLyTourDuLich.Api.Common.Exceptions;
using Xunit;

namespace QuanLyTourDuLich.Tests.Operations;

public sealed class ProfitCalculatorTests
{
    [Fact]
    public void CalculatesGrossProfitFromRevenueLessSupplierAndApprovedExtraCosts() =>
        Assert.Equal(650m, ProfitCalculator.Calculate(1200m, 400m, 150m));

    [Fact]
    public void SettledDepartureCannotBeEditedAndReturnsConflict()
    {
        var error = Assert.Throws<ConflictException>(() => SettlementLockRules.EnsureEditable("Settled"));
        Assert.Contains("khóa sổ", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonSettledDepartureRemainsEditable() => SettlementLockRules.EnsureEditable("Completed");
}
