namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public static class BookingStateRules
{
    private static readonly HashSet<string> EditableStatuses = ["PendingPayment", "DepositPaid", "Confirmed", "FinalPaymentOverdue"];

    public static bool CanReserve(int totalCapacity, int heldSlots, int bookedSlots, int availableSlots, int seats) =>
        seats > 0 && heldSlots >= 0 && bookedSlots >= 0 && availableSlots >= seats && totalCapacity >= heldSlots + bookedSlots + seats;

    public static bool CanAdjust(int totalCapacity, int heldSlots, int bookedSlots, int availableSlots, int delta) =>
        heldSlots >= 0 && bookedSlots >= 0 && availableSlots >= 0 &&
        (delta <= 0 || (availableSlots >= delta && heldSlots + bookedSlots + delta <= totalCapacity));

    public static bool CanEdit(string status) => EditableStatuses.Contains(status);
    public static bool CanRequestCancellation(string status) => EditableStatuses.Contains(status);
}
