using System.Text.Json;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public sealed record BookingPrice(decimal SubTotal, decimal Discount, decimal BeforeTax, decimal Vat, decimal Total, decimal Deposit, string TaxMode, decimal VatRate);

public static class BookingPricing
{
    public static decimal CalculateSubtotal(TourDepartures departure, int adults, int children, int infants, int singleRooms) =>
        adults * departure.AdultPrice + children * (departure.ChildPrice ?? departure.AdultPrice) +
        infants * (departure.InfantPrice ?? 0m) + singleRooms * (departure.SingleSupplement ?? 0m);

    public static BookingPrice Calculate(TourDepartures departure, int adults, int children, int infants, int singleRooms,
        decimal discount, decimal vatRate, string taxMode, decimal depositPercent = 30m)
    {
        if (adults < 1 || children < 0 || infants < 0 || singleRooms < 0 || discount < 0 || vatRate is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(adults), "Invalid passenger count or pricing input.");
        var subtotal = CalculateSubtotal(departure, adults, children, infants, singleRooms);
        discount = Math.Min(subtotal, discount);
        var net = subtotal - discount;
        var included = string.Equals(taxMode, "Included", StringComparison.OrdinalIgnoreCase);
        var beforeTax = included && vatRate > 0 ? Math.Round(net / (1m + vatRate / 100m), 2, MidpointRounding.AwayFromZero) : net;
        var vat = included ? net - beforeTax : Math.Round(net * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
        var total = included ? net : net + vat;
        return new BookingPrice(subtotal, discount, beforeTax, vat, total,
            Math.Round(total * depositPercent / 100m, 2, MidpointRounding.AwayFromZero), included ? "Included" : "Excluded", vatRate);
    }

    public static string PriceSnapshot(TourDepartures departure, BookingPrice price, int adults, int children, int infants, int singles) =>
        JsonSerializer.Serialize(new { departure.AdultPrice, departure.ChildPrice, departure.InfantPrice, departure.SingleSupplement,
            adults, children, infants, singleRooms = singles, price.SubTotal, price.Discount, price.BeforeTax, price.Vat, price.Total,
            price.Deposit, price.TaxMode, price.VatRate });
}
