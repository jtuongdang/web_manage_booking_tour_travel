namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class PromotionUsages
{
    public long Id { get; set; }
    public int PromotionId { get; set; }
    public long BookingId { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}
