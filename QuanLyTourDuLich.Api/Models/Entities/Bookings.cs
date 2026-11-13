namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Bookings
{
    public long Id { get; set; }
    public string BookingCode { get; set; } = null!;
    public int TourId { get; set; }
    public int DepartureId { get; set; }
    public int CustomerId { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? SalesUserId { get; set; }
    public string BookingChannel { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string ContactPhone { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public string? SpecialNote { get; set; } = null!;
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int InfantCount { get; set; }
    public int SingleRoomCount { get; set; }
    public int SeatsReserved { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string TaxMode { get; set; } = null!;
    public decimal VatRate { get; set; }
    public decimal AmountBeforeTax { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RequiredDepositAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int? PromotionId { get; set; }
    public string? PromoCodeApplied { get; set; } = null!;
    public string PayOption { get; set; } = null!;
    public bool RequireVatInvoice { get; set; }
    public string? InvoiceCompanyName { get; set; } = null!;
    public string? InvoiceTaxCode { get; set; } = null!;
    public string? InvoiceAddress { get; set; } = null!;
    public string? InvoiceEmail { get; set; } = null!;
    public string PriceSnapshotJson { get; set; } = null!;
    public string CancellationPolicySnapshotJson { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime HoldExpiresAt { get; set; }
    public DateTime? FinalPaymentDueAt { get; set; }
    public string? CancellationReason { get; set; } = null!;
    public decimal? CancelPenaltyPercent { get; set; }
    public decimal? CancelPenaltyAmount { get; set; }
    public int? CancelApprovedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
