namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Notifications
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string? TargetRole { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? LinkUrl { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
