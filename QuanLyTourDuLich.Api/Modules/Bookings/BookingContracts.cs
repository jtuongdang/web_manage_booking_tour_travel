namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public sealed record CreateBookingRequest(int DepartureId, string ContactName, string ContactPhone, string? ContactEmail,
    int AdultCount, int ChildCount = 0, int InfantCount = 0, int SingleRoomCount = 0, string BookingChannel = "WebB2C",
    string? SpecialNote = null, string? PromoCode = null, string PayOption = "Full", bool RequireVatInvoice = false);
public sealed record PassengerRequest(string PaxType, string FullName, string Nationality = "VN", string? Gender = null,
    DateOnly? DateOfBirth = null, string? IdentityNumber = null, DateOnly? PassportExpiryDate = null, string? SpecialRequest = null);
public sealed record AdjustPaxRequest(int AdultCount, int ChildCount, int InfantCount = 0, int SingleRoomCount = 0, string? Reason = null);
public sealed record RescheduleRequest(int NewDepartureId, string? Reason = null);
public sealed record CancelRequest(string Reason);
