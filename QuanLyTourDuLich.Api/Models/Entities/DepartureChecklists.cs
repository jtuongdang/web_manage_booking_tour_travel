namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class DepartureChecklists
{
    public int Id { get; set; }
    public int DepartureId { get; set; }
    public string TaskName { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public int? CompletedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; } = null!;
    public int SortOrder { get; set; }
}
