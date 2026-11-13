namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class TourIncidents
{
    public long Id { get; set; }
    public int DepartureId { get; set; }
    public int ReportedByUserId { get; set; }
    public string Title { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ResolutionNote { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
