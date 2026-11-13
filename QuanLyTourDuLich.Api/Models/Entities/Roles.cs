namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Roles
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
