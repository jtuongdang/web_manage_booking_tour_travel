namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class BookingPassengers
{
    public long Id { get; set; }
    public long BookingId { get; set; }
    public string PaxType { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? IdentityNumber { get; set; } = null!;
    public DateOnly? PassportExpiryDate { get; set; }
    public string Nationality { get; set; } = null!;
    public string? SpecialRequest { get; set; } = null!;
    public string CheckInStatus { get; set; } = null!;
    public DateTime? CheckedInAt { get; set; }
    public int? CheckedInByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
