using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.Operations;
using SupplierEntity = QuanLyTourDuLich.Api.Models.Entities.Suppliers;

namespace QuanLyTourDuLich.Api.Modules.Suppliers;

[ApiController, Authorize]
[Route("api/v1")]
public sealed class SuppliersController(AppDbContext db, ICurrentUserService current, IAuditLogService audit, IFinancialLedgerWriter ledger) : ControllerBase
{
    [HttpGet("suppliers")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [HttpPost("suppliers")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Create(SupplierRequest r, CancellationToken ct)
    {
        ValidateSupplier(r);
        var e = new SupplierEntity(); Apply(e, r); db.Suppliers.Add(e); await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CreateSupplier", "Suppliers", e.Id.ToString(), newValue: e, cancellationToken: ct);
        return Created($"/api/v1/suppliers/{e.Id}", e);
    }

    [HttpGet("suppliers/{id:int}")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await db.Suppliers.FindAsync([id], ct) ?? throw new NotFoundException("Không tìm thấy nhà cung cấp."));

    [HttpPut("suppliers/{id:int}")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Update(int id, SupplierRequest r, CancellationToken ct)
    {
        ValidateSupplier(r); var e = await db.Suppliers.FindAsync([id], ct) ?? throw new NotFoundException("Không tìm thấy nhà cung cấp."); var old = new { e.Name, e.SupplierType, e.IsActive };
        Apply(e, r); await db.SaveChangesAsync(ct); await audit.WriteAsync("UpdateSupplier", "Suppliers", id.ToString(), old, e, cancellationToken: ct); return Ok(e);
    }

    [HttpDelete("suppliers/{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await db.Suppliers.FindAsync([id], ct) ?? throw new NotFoundException("Không tìm thấy nhà cung cấp.");
        if (await db.DepartureServices.AnyAsync(x => x.SupplierId == id, ct)) throw new ConflictException("Nhà cung cấp đã được dùng trong dịch vụ; hãy vô hiệu hóa thay vì xóa.");
        db.Suppliers.Remove(e); await db.SaveChangesAsync(ct); await audit.WriteAsync("DeleteSupplier", "Suppliers", id.ToString(), oldValue: e, cancellationToken: ct); return NoContent();
    }

    [HttpGet("departures/{id:int}/services")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Services(int id, CancellationToken ct) { _ = await Departure(id, ct); return Ok(await db.DepartureServices.AsNoTracking().Where(x => x.DepartureId == id).OrderBy(x => x.Id).ToListAsync(ct)); }

    [HttpPost("departures/{id:int}/services")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> AddService(int id, DepartureServiceRequest r, CancellationToken ct)
    {
        var d = await Departure(id, ct); EnsureOpen(d); ValidateService(r);
        if (!await db.Suppliers.AnyAsync(x => x.Id == r.SupplierId && x.IsActive, ct)) throw new NotFoundException("Không tìm thấy nhà cung cấp đang hoạt động.");
        var e = new DepartureServices { DepartureId = id, SupplierId = r.SupplierId, ServiceName = r.ServiceName.Trim(), Quantity = r.Quantity, UnitPrice = r.UnitPrice, ActualCost = r.ActualCost, PaymentStatus = "Unpaid", BookingReference = r.BookingReference, Notes = r.Notes };
        db.DepartureServices.Add(e); await db.SaveChangesAsync(ct); await audit.WriteAsync("AddDepartureService", "DepartureServices", e.Id.ToString(), newValue: e, cancellationToken: ct); return Created($"/api/v1/departure-services/{e.Id}", e);
    }

    [HttpPut("departures/{id:int}/services")]
    [Authorize(Roles = RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<IActionResult> ReplaceServices(int id, IReadOnlyList<DepartureServiceRequest> requests, CancellationToken ct)
    {
        var d = await Departure(id, ct); EnsureOpen(d); foreach (var r in requests) ValidateService(r);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var old = await db.DepartureServices.Where(x => x.DepartureId == id).ToListAsync(ct);
        if (old.Any(x => x.PaidAmount > 0)) throw new ConflictException("Không thể thay thế dịch vụ đã có thanh toán.");
        db.DepartureServices.RemoveRange(old);
        foreach (var r in requests) db.DepartureServices.Add(new DepartureServices { DepartureId=id, SupplierId=r.SupplierId, ServiceName=r.ServiceName.Trim(), Quantity=r.Quantity, UnitPrice=r.UnitPrice, ActualCost=r.ActualCost, PaymentStatus="Unpaid", BookingReference=r.BookingReference, Notes=r.Notes });
        await db.SaveChangesAsync(ct); await audit.WriteAsync("ReplaceDepartureServices", "TourDepartures", id.ToString(), oldValue: old, newValue: requests, cancellationToken: ct); await tx.CommitAsync(ct); return Ok(await db.DepartureServices.Where(x=>x.DepartureId==id).ToListAsync(ct));
    }

    [HttpPost("departure-services/{id:int}/payments")]
    [Authorize(Roles = RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> Pay(int id, SupplierPaymentRequest r, CancellationToken ct)
    {
        if (r.Amount <= 0 || r.Method is not ("BankTransfer" or "Cash")) throw new BadRequestException("Số tiền phải dương và Method phải là BankTransfer hoặc Cash.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var s = await db.DepartureServices.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException("Không tìm thấy dịch vụ NCC."); var d=await Departure(s.DepartureId,ct); EnsureOpen(d);
        if (s.PaidAmount + r.Amount > s.ActualCost) throw new BadRequestException("Thanh toán vượt chi phí thực tế đã nghiệm thu.");
        var p = new SupplierPayments { PaymentCode=$"SP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(), DepartureServiceId=id, SupplierId=s.SupplierId, Amount=r.Amount, Method=r.Method, BankRef=r.BankRef, EvidenceUrl=r.EvidenceUrl, PaidAt=DateTime.UtcNow, PaidByUserId=current.UserId ?? throw new UnauthorizedException("Không xác định được người dùng."), Notes=r.Notes };
        db.SupplierPayments.Add(p); await db.SaveChangesAsync(ct); s.PaidAmount += r.Amount; s.PaymentStatus=s.PaidAmount >= s.ActualCost ? "Paid" : "Partial";
        await ledger.AppendAsync(new FinancialLedgerAppendRequest("Out","SupplierPayment",r.Amount,$"Thanh toán dịch vụ {s.ServiceName}",s.DepartureId,SupplierPaymentId:p.Id,RecordedByUserId:current.UserId), cancellationToken:ct);
        await db.SaveChangesAsync(ct); await audit.WriteAsync("PaySupplierService", "SupplierPayments", p.Id.ToString(), newValue: p, cancellationToken: ct); await tx.CommitAsync(ct); return Created($"/api/v1/supplier-payments/{p.Id}",p);
    }

    private async Task<TourDepartures> Departure(int id,CancellationToken ct)=> await db.TourDepartures.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException("Không tìm thấy lịch khởi hành.");
    private static void EnsureOpen(TourDepartures d) => SettlementLockRules.EnsureEditable(d.Status);
    private static void ValidateSupplier(SupplierRequest r) { if (string.IsNullOrWhiteSpace(r.Code)||string.IsNullOrWhiteSpace(r.Name)||!new[]{"Hotel","Transport","Airline","Restaurant","Ticket","Visa","Other"}.Contains(r.SupplierType)||r.InternalRating is <1 or >5) throw new BadRequestException("Thông tin nhà cung cấp hoặc SupplierType/InternalRating không hợp lệ."); }
    private static void Apply(SupplierEntity e,SupplierRequest r) { e.Code=r.Code.Trim();e.Name=r.Name.Trim();e.SupplierType=r.SupplierType;e.TaxCode=r.TaxCode;e.ContactName=r.ContactName;e.Phone=r.Phone;e.Email=r.Email;e.Address=r.Address;e.BankName=r.BankName;e.BankAccountNumber=r.BankAccountNumber;e.BankAccountHolder=r.BankAccountHolder;e.InternalRating=r.InternalRating;e.IsActive=r.IsActive;e.Notes=r.Notes; }
    private static void ValidateService(DepartureServiceRequest r) { if(r.SupplierId<=0||string.IsNullOrWhiteSpace(r.ServiceName)||r.Quantity<=0||r.UnitPrice<0||r.ActualCost<0) throw new BadRequestException("Thông tin dịch vụ hoặc chi phí không hợp lệ."); }
}
public sealed record SupplierRequest(string Code,string Name,string SupplierType,string? TaxCode=null,string? ContactName=null,string? Phone=null,string? Email=null,string? Address=null,string? BankName=null,string? BankAccountNumber=null,string? BankAccountHolder=null,byte? InternalRating=null,bool IsActive=true,string? Notes=null);
public sealed record DepartureServiceRequest(int SupplierId,string ServiceName,decimal Quantity,decimal UnitPrice,decimal ActualCost=0,string? BookingReference=null,string? Notes=null);
public sealed record SupplierPaymentRequest(decimal Amount,string Method,string? BankRef=null,string? EvidenceUrl=null,string? Notes=null);
