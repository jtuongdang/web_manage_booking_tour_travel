namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class TourReviews
{
    public long Id { get; set; }
    public int TourId { get; set; }
    public int DepartureId { get; set; }
    public long BookingId { get; set; }
    public int CustomerId { get; set; }
    public byte OverallRating { get; set; }
    public byte? GuideRating { get; set; } = null!;
    public byte? ItineraryRating { get; set; } = null!;
    public byte? HotelFoodRating { get; set; } = null!;
    public byte? TransportRating { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public string? ImagesJson { get; set; } = null!;
    public string? StaffReply { get; set; } = null!;
    public int? RepliedByUserId { get; set; }
    public DateTime? RepliedAt { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
}
