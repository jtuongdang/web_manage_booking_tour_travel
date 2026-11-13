namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class RefundRequests
{
    public long Id { get; set; }
    public string RefundCode { get; set; } = null!;
    public long BookingId { get; set; }
    public int? RequestedByUserId { get; set; }
    public string? Reason { get; set; } = null!;
    public decimal PaidAmountSnapshot { get; set; }
    public decimal PenaltyAmountSnapshot { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = null!;
    public string? BankName { get; set; } = null!;
    public string? BankAccountNumber { get; set; } = null!;
    public string? BankAccountHolder { get; set; } = null!;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? CompletedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? TransferRef { get; set; } = null!;
    public string? RejectReason { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
