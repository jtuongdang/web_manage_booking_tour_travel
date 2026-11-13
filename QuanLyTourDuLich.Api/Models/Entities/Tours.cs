namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Tours
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string TourType { get; set; } = null!;
    public string ScopeType { get; set; } = null!;
    public string? Tags { get; set; } = null!;
    public string DepartureFrom { get; set; } = null!;
    public string Destination { get; set; } = null!;
    public byte DurationDays { get; set; }
    public byte DurationNights { get; set; }
    public string Transport { get; set; } = null!;
    public decimal? OriginalPrice { get; set; }
    public decimal BaseAdultPrice { get; set; }
    public decimal ChildPricePercent { get; set; }
    public decimal InfantPrice { get; set; }
    public decimal SingleSupplement { get; set; }
    public int CancellationPolicyId { get; set; }
    public string? Summary { get; set; } = null!;
    public string? HighlightsJson { get; set; } = null!;
    public string? IncludesJson { get; set; } = null!;
    public string? ExcludesJson { get; set; } = null!;
    public string? Badges { get; set; } = null!;
    public bool IsPublic { get; set; }
    public string Status { get; set; } = null!;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int BookedCountTotal { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
