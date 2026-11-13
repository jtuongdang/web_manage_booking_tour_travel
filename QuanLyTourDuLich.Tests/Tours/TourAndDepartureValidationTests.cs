using QuanLyTourDuLich.Api.Modules.Departures;
using QuanLyTourDuLich.Api.Modules.Tours;
using Xunit;

namespace QuanLyTourDuLich.Tests.Tours;

public sealed class TourAndDepartureValidationTests
{
    [Theory]
    [InlineData("GroupTour", true)]
    [InlineData("PrivateTour", true)]
    [InlineData("CustomTour", true)]
    [InlineData("OneOff", false)]
    public void Accepts_only_schema_tour_types(string value, bool expected) => Assert.Equal(expected, TourValidation.IsKnownTourType(value));

    [Theory]
    [InlineData("Domestic", true)]
    [InlineData("International", true)]
    [InlineData("Regional", false)]
    public void Accepts_only_schema_scope_types(string value, bool expected) => Assert.Equal(expected, TourValidation.IsKnownScope(value));

    [Theory]
    [InlineData("[\"a\"]", true)]
    [InlineData("{\"b\":1}", true)]
    [InlineData("not-json", false)]
    [InlineData(null, true)]
    public void Validates_tour_json_columns(string? value, bool expected) => Assert.Equal(expected, TourValidation.IsValidJson(value));

    [Theory]
    [InlineData(10, 3, 7, true)]
    [InlineData(10, 3, 8, false)]
    [InlineData(0, 0, 0, false)]
    [InlineData(10, -1, 0, false)]
    public void Enforces_non_overbooking_capacity(int total, int held, int booked, bool expected) =>
        Assert.Equal(expected, DepartureValidation.IsValidCapacity(total, held, booked));

    [Theory]
    [InlineData("123456789012", "********9012")]
    [InlineData("1234", "1234")]
    [InlineData(null, null)]
    public void Masks_identity_numbers_for_limited_manifest_access(string? identity, string? expected) =>
        Assert.Equal(expected, DepartureValidation.MaskIdentity(identity));
}
