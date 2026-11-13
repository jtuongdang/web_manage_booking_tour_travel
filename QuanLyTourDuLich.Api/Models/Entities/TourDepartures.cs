namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class TourDepartures
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public string DepartureCode { get; set; } = null!;
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public DateTime CutoffAt { get; set; }
    public decimal AdultPrice { get; set; }
    public decimal? ChildPrice { get; set; }
    public decimal? InfantPrice { get; set; }
    public decimal? SingleSupplement { get; set; }
    public int TotalCapacity { get; set; }
    public int HeldSlots { get; set; }
    public int BookedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int MinParticipants { get; set; }
    public int? MaxInfants { get; set; }
    public int? CancellationPolicyId { get; set; }
    public bool IsPublic { get; set; }
    public string? ContractCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? VehicleType { get; set; } = null!;
    public string? VehiclePlate { get; set; } = null!;
    public string? DriverName { get; set; } = null!;
    public string? DriverPhone { get; set; } = null!;
    public string? Notes { get; set; } = null!;
    public DateTime? SettledAt { get; set; }
    public int? SettledByUserId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
