using System.Net;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;

namespace QuanLyTourDuLich.Api.Modules.Finance;

public sealed class SimulatedTaxInvoiceService(AppDbContext db) : ITaxInvoiceService
{
    public async Task<SimulatedInvoiceRenderResult> RenderSimulatedInvoiceHtmlAsync(
        long bookingId,
        string invoiceNumber,
        string serialNumber,
        string lookupCode,
        CancellationToken cancellationToken = default)
    {
        var invoice = await db.SimulatedInvoices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var html = $"""<!doctype html><html lang="vi"><meta charset="utf-8"><title>Hóa đơn {E(invoice.InvoiceNumber)}</title><body><main><h1>HÓA ĐƠN GIÁ TRỊ GIA TĂNG (MÔ PHỎNG)</h1><strong style="color:#b00020">HÓA ĐƠN MÔ PHỎNG - KHÔNG CÓ GIÁ TRỊ PHÁP LÝ</strong><p>Số: {E(invoice.InvoiceNumber)} · Ký hiệu: {E(invoice.SerialNumber)}</p><p>Mã tra cứu: {E(invoice.LookupCode)}</p><hr><p>Người mua: {E(invoice.BuyerName)}</p><p>Mã số thuế: {E(invoice.BuyerTaxCode)}</p><p>Địa chỉ: {E(invoice.BuyerAddress)}</p><p>Email: {E(invoice.BuyerEmail)}</p><p>Tiền trước thuế: {invoice.AmountBeforeTax:N0} VND</p><p>Thuế suất VAT: {invoice.VatRate:N2}%</p><p>Tiền VAT: {invoice.VatAmount:N0} VND</p><h2>Tổng thanh toán: {invoice.TotalAmount:N0} VND</h2><p>Trạng thái: {E(invoice.Status)}</p></main></body></html>""";
        return new SimulatedInvoiceRenderResult(invoiceNumber, serialNumber, lookupCode, html);
    }
}
