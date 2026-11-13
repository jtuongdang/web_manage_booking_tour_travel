namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class FinancialLedgers
{
    public long Id { get; set; }
    public string EntryCode { get; set; } = null!;
    public string Direction { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public int? DepartureId { get; set; }
    public long? BookingId { get; set; }
    public long? PaymentTransactionId { get; set; }
    public long? RefundRequestId { get; set; }
    public long? SupplierPaymentId { get; set; }
    public long? ExtraExpenseId { get; set; }
    public string Description { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public int? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
