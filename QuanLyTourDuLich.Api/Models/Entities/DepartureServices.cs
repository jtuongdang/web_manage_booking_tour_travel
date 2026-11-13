namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class DepartureServices
{
    public int Id { get; set; }
    public int DepartureId { get; set; }
    public int SupplierId { get; set; }
    public string ServiceName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public string? BookingReference { get; set; } = null!;
    public string? Notes { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
