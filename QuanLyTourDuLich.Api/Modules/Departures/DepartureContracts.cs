using System.ComponentModel.DataAnnotations;

namespace QuanLyTourDuLich.Api.Modules.Departures;

public sealed class DepartureWriteRequest
{
    [Range(1, int.MaxValue)] public int TourId { get; init; }
    [Required, StringLength(50)] public string DepartureCode { get; init; } = "";
    public DateOnly DepartureDate { get; init; }
    public DateOnly ReturnDate { get; init; }
    public DateTime CutoffAt { get; init; }
    [Range(0, double.MaxValue)] public decimal AdultPrice { get; init; }
    [Range(0, double.MaxValue)] public decimal? ChildPrice { get; init; }
    [Range(0, double.MaxValue)] public decimal? InfantPrice { get; init; }
    [Range(0, double.MaxValue)] public decimal? SingleSupplement { get; init; }
    [Range(1, int.MaxValue)] public int TotalCapacity { get; init; }
    [Range(1, int.MaxValue)] public int MinParticipants { get; init; } = 1;
    [Range(0, int.MaxValue)] public int? MaxInfants { get; init; }
    public int? CancellationPolicyId { get; init; }
    public bool IsPublic { get; init; } = true;
    public string? ContractCode { get; init; }
    public string? VehicleType { get; init; }
    public string? VehiclePlate { get; init; }
    public string? DriverName { get; init; }
    public string? DriverPhone { get; init; }
    public string? Notes { get; init; }
}

public sealed record DepartureStatusRequest([Required] string Status);
public sealed record ManifestPassenger(long BookingId, string BookingCode, string BookingStatus, string PaxType,
    string FullName, string? IdentityNumber, string? Nationality, string? CheckInStatus);

public static class DepartureValidation
{
    public static readonly string[] Statuses = ["Open", "Full", "Confirmed", "InProgress", "Completed", "Settled", "Cancelled"];
    public static bool IsValidCapacity(int capacity, int held, int booked) => capacity >= 1 && held >= 0 && booked >= 0 && held + booked <= capacity;
    public static string? MaskIdentity(string? value) => string.IsNullOrWhiteSpace(value) ? value : new string('*', Math.Max(0, value.Length - 4)) + value[^Math.Min(4, value.Length)..];
}
