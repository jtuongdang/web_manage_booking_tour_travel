namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Customers
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = null!;
    public int? UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; } = null!;
    public string? Address { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string LoyaltyTier { get; set; } = null!;
    public decimal TotalSpent { get; set; }
    public int TotalBookings { get; set; }
    public string? Notes { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
