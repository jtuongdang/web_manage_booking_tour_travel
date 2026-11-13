namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class SystemSettings
{
    public string SettingKey { get; set; } = null!;
    public string SettingValue { get; set; } = null!;
    public string ValueType { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
