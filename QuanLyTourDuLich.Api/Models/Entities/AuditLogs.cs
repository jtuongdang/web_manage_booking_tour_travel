namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class AuditLogs
{
    public long Id { get; set; }
    public int? ActorUserId { get; set; }
    public string? ActorRole { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValueJson { get; set; } = null!;
    public string? NewValueJson { get; set; } = null!;
    public string? IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
