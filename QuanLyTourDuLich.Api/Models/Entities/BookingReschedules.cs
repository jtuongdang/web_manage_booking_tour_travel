namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class BookingReschedules
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public int OldDepartureId { get; set; }
    public int NewDepartureId { get; set; }
    public decimal OldTotalAmount { get; set; }
    public decimal NewTotalAmount { get; set; }
    public decimal RescheduleFee { get; set; }
    public decimal PriceDifference { get; set; }
    public string? Reason { get; set; } = null!;
    public int? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
