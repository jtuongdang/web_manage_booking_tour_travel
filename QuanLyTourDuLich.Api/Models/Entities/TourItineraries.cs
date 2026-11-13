namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class TourItineraries
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public byte DayNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Meals { get; set; } = null!;
    public string? Accommodation { get; set; } = null!;
    public string Description { get; set; } = null!;
}
