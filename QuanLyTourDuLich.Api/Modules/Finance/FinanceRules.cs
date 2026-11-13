using System.Text.Json;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Finance;

public static class FinanceRules
{
    public static decimal CalculateRefundAmount(decimal paidAmount, decimal penaltyAmount) =>
        Math.Max(0m, paidAmount - penaltyAmount);

    public static decimal CalculatePenalty(Bookings booking, decimal penaltyPercent, bool companyCancelled)
    {
        if (companyCancelled) return 0m;
        var percent = booking.CancelPenaltyPercent ?? penaltyPercent;
        return Math.Round(booking.TotalAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculatePenaltyFromSnapshot(Bookings booking, DateOnly departureDate, DateTime now)
    {
        if (booking.CancelPenaltyAmount.HasValue) return booking.CancelPenaltyAmount.Value;
        if (booking.CancelPenaltyPercent.HasValue)
            return CalculatePenalty(booking, booking.CancelPenaltyPercent.Value, false);

        using var json = JsonDocument.Parse(booking.CancellationPolicySnapshotJson);
        var root = json.RootElement;
        if (TryGetDecimal(root, "penaltyPercent", out var fixedPercent)
            || TryGetDecimal(root, "PenaltyPercent", out fixedPercent))
            return CalculatePenalty(booking, fixedPercent, false);

        var days = Math.Max(0, departureDate.DayNumber - DateOnly.FromDateTime(now).DayNumber);
        var rules = FindArray(root, "rules", "Rules", "cancellationRules", "CancellationRules");
        if (rules.HasValue)
        {
            var selected = rules.Value.EnumerateArray()
                .Select(rule => new
                {
                    Min = GetInt(rule, "minDaysBeforeDeparture", "MinDaysBeforeDeparture"),
                    Max = GetNullableInt(rule, "maxDaysBeforeDeparture", "MaxDaysBeforeDeparture"),
                    Percent = GetDecimal(rule, "penaltyPercent", "PenaltyPercent")
                })
                .Where(x => days >= x.Min && (!x.Max.HasValue || days <= x.Max.Value))
                .OrderByDescending(x => x.Min)
                .FirstOrDefault();
            if (selected is not null) return CalculatePenalty(booking, selected.Percent, false);
        }

        throw new InvalidOperationException("Không tìm thấy mức phí phạt áp dụng trong snapshot chính sách hủy.");
    }

    private static JsonElement? FindArray(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array) return value;
        return null;
    }

    private static bool TryGetDecimal(JsonElement element, string name, out decimal value)
    {
        if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out value)) return true;
        value = 0m;
        return false;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (TryGetDecimal(element, name, out var value)) return value;
        return 0m;
    }

    private static int GetInt(JsonElement element, params string[] names) => GetNullableInt(element, names) ?? 0;

    private static int? GetNullableInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)) return value;
        return null;
    }
}
