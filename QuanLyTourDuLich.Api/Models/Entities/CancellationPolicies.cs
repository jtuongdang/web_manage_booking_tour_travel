namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class CancellationPolicies
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public decimal CompanyCancelRefundPercent { get; set; }
    public bool AllowReschedule { get; set; }
    public int RescheduleCutoffDays { get; set; }
    public decimal RescheduleFeePercent { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
