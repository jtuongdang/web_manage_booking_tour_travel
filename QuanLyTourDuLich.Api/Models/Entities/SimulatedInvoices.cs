namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class SimulatedInvoices
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string LookupCode { get; set; } = null!;
    public string BuyerType { get; set; } = null!;
    public string BuyerName { get; set; } = null!;
    public string? BuyerTaxCode { get; set; } = null!;
    public string? BuyerAddress { get; set; } = null!;
    public string? BuyerEmail { get; set; } = null!;
    public decimal AmountBeforeTax { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public int? IssuedByUserId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
