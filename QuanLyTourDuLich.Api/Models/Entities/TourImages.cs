namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class TourImages
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? AltText { get; set; } = null!;
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
}
