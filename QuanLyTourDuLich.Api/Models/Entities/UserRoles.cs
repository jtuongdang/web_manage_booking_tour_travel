namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class UserRoles
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedByUserId { get; set; }
}
