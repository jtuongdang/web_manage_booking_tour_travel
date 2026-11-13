namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public static class BookingStateRules
{
    private static readonly HashSet<string> EditableStatuses = ["PendingPayment", "DepositPaid", "Confirmed", "FinalPaymentOverdue"];

    public static int SeatsCount(int adultCount, int childCount) => Math.Max(0, adultCount) + Math.Max(0, childCount);

    public static int RemainingCapacity(int totalCapacity, int heldSlots, int bookedSlots) =>
        Math.Max(0, totalCapacity - heldSlots - bookedSlots);

    public static bool CanReserve(int totalCapacity, int heldSlots, int bookedSlots, int seats) =>
        seats > 0 && heldSlots >= 0 && bookedSlots >= 0 && RemainingCapacity(totalCapacity, heldSlots, bookedSlots) >= seats;

    public static bool CanReserve(int totalCapacity, int heldSlots, int bookedSlots, int availableSlots, int seats) =>
        seats > 0 && heldSlots >= 0 && bookedSlots >= 0 && availableSlots >= seats && totalCapacity >= heldSlots + bookedSlots + seats;

    public static bool CanAdjust(int totalCapacity, int heldSlots, int bookedSlots, int delta) =>
        heldSlots >= 0 && bookedSlots >= 0 && RemainingCapacity(totalCapacity, heldSlots, bookedSlots) >= Math.Max(0, delta);

    public static bool CanAdjust(int totalCapacity, int heldSlots, int bookedSlots, int availableSlots, int delta) =>
        heldSlots >= 0 && bookedSlots >= 0 && availableSlots >= 0 &&
        (delta <= 0 || (availableSlots >= delta && heldSlots + bookedSlots + delta <= totalCapacity));

    public static bool ShouldReleaseBookedSlots(decimal paidAmount, decimal requiredDepositAmount) =>
        paidAmount > 0m && paidAmount >= requiredDepositAmount;

    public static bool CanEdit(string status) => EditableStatuses.Contains(status);
    public static bool CanRequestCancellation(string status) => EditableStatuses.Contains(status);
}
