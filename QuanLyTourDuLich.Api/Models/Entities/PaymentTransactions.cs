namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class PaymentTransactions
{
    public long Id { get; set; }
    public string TransactionCode { get; set; } = null!;
    public long BookingId { get; set; }
    public int? CompanyBankAccountId { get; set; }
    public string Method { get; set; } = null!;
    public string? TransferContent { get; set; } = null!;
    public string? QrPayload { get; set; } = null!;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualReceivedAmount { get; set; }
    public string Status { get; set; } = null!;
    public string ReconciliationStatus { get; set; } = null!;
    public string? IdempotencyKey { get; set; } = null!;
    public string? BankCode { get; set; } = null!;
    public string? BankRef { get; set; } = null!;
    public string? EvidenceUrl { get; set; } = null!;
    public int? ReconciledByUserId { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciliationNote { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? RawCallbackJson { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
