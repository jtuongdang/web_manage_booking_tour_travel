namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class DepartureExtraExpenses
{
    public long Id { get; set; }
    public int DepartureId { get; set; }
    public string Title { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? AttachmentUrl { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public string ApprovalStatus { get; set; } = null!;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
