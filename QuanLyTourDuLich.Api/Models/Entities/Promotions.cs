namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Promotions
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public string DiscountType { get; set; } = null!;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int MinPax { get; set; }
    public bool ApplyAllTours { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public int MaxUsagePerCustomer { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
