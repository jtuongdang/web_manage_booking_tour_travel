namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class RefreshTokens
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; } = null!;
    public string? CreatedByIp { get; set; } = null!;
    public string? UserAgent { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
