namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class SupplierPayments
{
    public long Id { get; set; }
    public string PaymentCode { get; set; } = null!;
    public int DepartureServiceId { get; set; }
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = null!;
    public string? BankRef { get; set; } = null!;
    public string? EvidenceUrl { get; set; } = null!;
    public DateTime PaidAt { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
