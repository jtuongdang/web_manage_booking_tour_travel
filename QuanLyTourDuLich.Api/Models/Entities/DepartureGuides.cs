namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class DepartureGuides
{
    public int Id { get; set; }
    public int DepartureId { get; set; }
    public int GuideUserId { get; set; }
    public string GuideRole { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public int? AssignedByUserId { get; set; }
    public string? Notes { get; set; } = null!;
}
