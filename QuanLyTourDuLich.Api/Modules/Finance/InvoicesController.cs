using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Finance;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public sealed class InvoicesController(
    AppDbContext db,
    ISystemSettingService settings,
    ITaxInvoiceService taxInvoiceService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("/api/v1/bookings/{bookingId:long}/invoice-issue")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<ActionResult<SimulatedInvoices>> Issue(long bookingId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy booking.");
        if (booking.Status is not ("Confirmed" or "Completed") || booking.PaidAmount < booking.TotalAmount)
            throw new BadRequestException("Chỉ booking đã thanh toán đủ và ở trạng thái Confirmed hoặc Completed mới được xuất hóa đơn.");
        if (await db.SimulatedInvoices.AnyAsync(x => x.BookingId == bookingId, cancellationToken))
            throw new ConflictException("Booking đã có hóa đơn mô phỏng.");

        var issuedAt = DateTime.UtcNow;
        var year = issuedAt.Year;
        var prefix = $"INV-{year}-";
        var issuedCount = await db.SimulatedInvoices.CountAsync(x => x.InvoiceNumber.StartsWith(prefix), cancellationToken);
        var invoiceNumber = $"{prefix}{issuedCount + 1:000000}";
        var serial = await settings.GetStringAsync("InvoiceSerialNumber", $"1C{year % 100:00}TVT", cancellationToken);
        var invoice = new SimulatedInvoices
        {
            BookingId = booking.Id,
            InvoiceNumber = invoiceNumber,
            SerialNumber = serial,
            LookupCode = Guid.NewGuid().ToString("N").ToUpperInvariant()[..16],
            BuyerType = booking.RequireVatInvoice ? "B2B" : "B2C",
            BuyerName = booking.RequireVatInvoice && !string.IsNullOrWhiteSpace(booking.InvoiceCompanyName)
                ? booking.InvoiceCompanyName : booking.ContactName,
            BuyerTaxCode = booking.RequireVatInvoice ? booking.InvoiceTaxCode : null,
            BuyerAddress = booking.RequireVatInvoice ? booking.InvoiceAddress : null,
            BuyerEmail = booking.RequireVatInvoice ? booking.InvoiceEmail : booking.ContactEmail,
            AmountBeforeTax = booking.AmountBeforeTax,
            VatRate = booking.VatRate,
            VatAmount = booking.VatAmount,
            TotalAmount = booking.TotalAmount,
            Status = "Issued",
            IssuedByUserId = currentUser.UserId,
            IssuedAt = issuedAt,
            CreatedAt = issuedAt
        };
        db.SimulatedInvoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [HttpGet]
    [Authorize(Roles = RoleCodes.Accountant + "," + RoleCodes.Admin + "," + RoleCodes.Sales)]
    public async Task<ActionResult<PagedResult<SimulatedInvoices>>> Get([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.SimulatedInvoices.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new PagedResult<SimulatedInvoices>(items, pageIndex, pageSize, total));
    }

    [HttpGet("/api/v1/bookings/{bookingId:long}/invoice")]
    [Authorize]
    public async Task<ActionResult<SimulatedInvoices>> GetByBookingId(long bookingId, CancellationToken cancellationToken)
    {
        await EnsureCanAccessInvoiceAsync(bookingId, cancellationToken);
        var invoice = await db.SimulatedInvoices.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking chưa được xuất hóa đơn mô phỏng.");
        return Ok(invoice);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<SimulatedInvoices>> GetById(long id, CancellationToken cancellationToken)
    {
        var invoice = await db.SimulatedInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");
        await EnsureCanAccessInvoiceAsync(invoice.BookingId, cancellationToken);
        return Ok(invoice);
    }

    [HttpGet("{id:long}/preview")]
    [Authorize]
    public async Task<IActionResult> Preview(long id, CancellationToken cancellationToken)
    {
        var invoice = await db.SimulatedInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");
        await EnsureCanAccessInvoiceAsync(invoice.BookingId, cancellationToken);
        var rendered = await taxInvoiceService.RenderSimulatedInvoiceHtmlAsync(invoice.BookingId, invoice.InvoiceNumber, invoice.SerialNumber, invoice.LookupCode, cancellationToken);
        return Content(rendered.HtmlContent, "text/html; charset=utf-8");
    }

    private async Task EnsureCanAccessInvoiceAsync(long bookingId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleCodes.Accountant) || User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.Sales))
        {
            return;
        }

        var customerId = await db.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(b => (int?)b.CustomerId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy booking.");

        if (currentUser.CustomerId == customerId)
        {
            return;
        }

        if (currentUser.UserId.HasValue && await db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId && c.UserId == currentUser.UserId.Value, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Bạn không có quyền xem hóa đơn của booking này.");
    }
}
