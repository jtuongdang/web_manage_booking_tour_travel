using System.ComponentModel.DataAnnotations;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Tours;

public sealed record TourQuery(
    string? Keyword = null, string? Category = null, string? Tags = null,
    string? ScopeType = null, string? DepartureFrom = null, string? Destination = null,
    decimal? MinPrice = null, decimal? MaxPrice = null, int? DurationDays = null,
    DateOnly? DepartureDate = null, bool IncludeInternal = false, int PageIndex = 1, int PageSize = 20);

public sealed record TourListItem(int Id, string Code, string Slug, string Title, string TourType,
    string ScopeType, string? Tags, string DepartureFrom, string Destination, byte DurationDays,
    decimal BaseAdultPrice, bool IsPublic, string Status, decimal AverageRating, int ReviewCount,
    DateOnly? NextDepartureDate, int? AvailableSlots);

public sealed record TourDetail(QuanLyTourDuLich.Api.Models.Entities.Tours Tour, IReadOnlyList<TourItineraries> Itineraries,
    IReadOnlyList<TourImages> Images, CancellationPolicyDetail CancellationPolicy,
    IReadOnlyList<TourDepartures> UpcomingDepartures, object ReviewsSummary);

public sealed record CancellationPolicyDetail(CancellationPolicies Policy, IReadOnlyList<CancellationPolicyRules> Rules);

public sealed class TourWriteRequest
{
    [Required, StringLength(40)] public string Code { get; init; } = "";
    [Required, StringLength(160)] public string Slug { get; init; } = "";
    [Required, StringLength(250)] public string Title { get; init; } = "";
    [Required] public string TourType { get; init; } = "GroupTour";
    [Required] public string ScopeType { get; init; } = "Domestic";
    public string? Tags { get; init; }
    [Required] public string DepartureFrom { get; init; } = "";
    [Required] public string Destination { get; init; } = "";
    [Range(1, 255)] public byte DurationDays { get; init; }
    [Range(0, 255)] public byte DurationNights { get; init; }
    [Required] public string Transport { get; init; } = "";
    [Range(0, double.MaxValue)] public decimal? OriginalPrice { get; init; }
    [Range(0, double.MaxValue)] public decimal BaseAdultPrice { get; init; }
    [Range(0, 100)] public decimal ChildPricePercent { get; init; } = 75;
    [Range(0, double.MaxValue)] public decimal InfantPrice { get; init; }
    [Range(0, double.MaxValue)] public decimal SingleSupplement { get; init; }
    [Range(1, int.MaxValue)] public int CancellationPolicyId { get; init; }
    public string? Summary { get; init; }
    public string? HighlightsJson { get; init; }
    public string? IncludesJson { get; init; }
    public string? ExcludesJson { get; init; }
    public string? Badges { get; init; }
    public bool IsPublic { get; init; }
}

public sealed record ItineraryWriteRequest([Required] IReadOnlyList<ItineraryItem> Items);
public sealed record ItineraryItem([Range(1, 255)] byte DayNumber, [Required] string Title,
    string? Meals, string? Accommodation, [Required] string Description);
public sealed record ImageWriteRequest([Required] IReadOnlyList<ImageItem> Items);
public sealed record ImageItem([Required] string ImageUrl, string? AltText, bool IsCover, int SortOrder);

public sealed class PolicyWriteRequest
{
    [Required, StringLength(40)] public string Code { get; init; } = "";
    [Required, StringLength(150)] public string Name { get; init; } = "";
    public string? Description { get; init; }
    [Range(0, 100)] public decimal CompanyCancelRefundPercent { get; init; } = 100;
    public bool AllowReschedule { get; init; }
    [Range(0, int.MaxValue)] public int RescheduleCutoffDays { get; init; }
    [Range(0, 100)] public decimal RescheduleFeePercent { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<PolicyRuleItem> Rules { get; init; } = [];
}
public sealed record PolicyRuleItem([Range(0, int.MaxValue)] int MinDaysBeforeDeparture,
    int? MaxDaysBeforeDeparture, [Range(0, 100)] decimal PenaltyPercent, [Required] string Label, int SortOrder);

public static class TourValidation
{
    public static bool IsKnownTourType(string value) => value is "GroupTour" or "PrivateTour" or "CustomTour";
    public static bool IsKnownScope(string value) => value is "Domestic" or "International";
    public static bool IsValidJson(string? value)
    {
        if (value is null) return true;
        try { System.Text.Json.JsonDocument.Parse(value); return true; }
        catch (System.Text.Json.JsonException) { return false; }
    }
}
