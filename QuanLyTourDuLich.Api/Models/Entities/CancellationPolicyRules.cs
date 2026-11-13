namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class CancellationPolicyRules
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int MinDaysBeforeDeparture { get; set; }
    public int? MaxDaysBeforeDeparture { get; set; }
    public decimal PenaltyPercent { get; set; }
    public string Label { get; set; } = null!;
    public int SortOrder { get; set; }
}
