using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Reports;

[ApiController,Authorize,Route("api/v1/reports")]
public sealed class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("revenue"),Authorize(Roles=RoleCodes.Accountant+","+RoleCodes.Admin)]
    public async Task<IActionResult> Revenue([FromQuery]DateTime? from,[FromQuery]DateTime? to,CancellationToken ct){var q=DateFilter(db.FinancialLedgers.AsNoTracking(),from,to);var rows=await q.ToListAsync(ct);var bookings=await DateFilter(db.Bookings.AsNoTracking(),from,to).ToListAsync(ct);var paid=rows.Where(x=>x.Direction=="In"&&x.Category=="CustomerPayment").Sum(x=>x.Amount);var refunds=rows.Where(x=>x.Direction=="Out"&&x.Category=="CustomerRefund").Sum(x=>x.Amount);return Ok(new{RecognizedRevenue=paid-refunds,Paid=paid,RemainingReceivable=bookings.Sum(x=>x.RemainingAmount),Refunded=refunds,VatAmount=bookings.Sum(x=>x.VatAmount),SupplierPayments=rows.Where(x=>x.Category=="SupplierPayment").Sum(x=>x.Amount),ExtraExpenses=rows.Where(x=>x.Category=="ExtraExpense").Sum(x=>x.Amount),NetCashFlow=rows.Sum(x=>x.Direction=="In"?x.Amount:-x.Amount)});}

    [HttpGet("departure-profit"),Authorize(Roles=RoleCodes.Accountant+","+RoleCodes.Admin+","+RoleCodes.Operator)]
    public async Task<IActionResult> DepartureProfit([FromQuery]DateOnly? from,[FromQuery]DateOnly? to,[FromQuery]int? tourId,CancellationToken ct){var q=db.TourDepartures.AsNoTracking().AsQueryable();if(from.HasValue)q=q.Where(x=>x.DepartureDate>=from.Value);if(to.HasValue)q=q.Where(x=>x.DepartureDate<=to.Value);if(tourId.HasValue)q=q.Where(x=>x.TourId==tourId);var data=await q.Select(d=>new{d.Id,d.DepartureCode,d.DepartureDate,d.Status,d.TourId,Revenue=db.FinancialLedgers.Where(l=>l.DepartureId==d.Id&&l.Direction=="In"&&l.Category=="CustomerPayment").Sum(l=>(decimal?)l.Amount)??0m,ServiceCost=db.DepartureServices.Where(s=>s.DepartureId==d.Id).Sum(s=>(decimal?)s.ActualCost)??0m,Extras=db.DepartureExtraExpenses.Where(e=>e.DepartureId==d.Id&&e.ApprovalStatus=="Approved").Sum(e=>(decimal?)e.Amount)??0m}).ToListAsync(ct);return Ok(data.Select(x=>new{x.Id,x.DepartureCode,x.DepartureDate,x.Status,x.TourId,x.Revenue,x.ServiceCost,x.Extras,GrossProfit=ProfitCalculator.Calculate(x.Revenue,x.ServiceCost,x.Extras),MarginPercent=x.Revenue==0?0:ProfitCalculator.Calculate(x.Revenue,x.ServiceCost,x.Extras)/x.Revenue*100}));}

    [HttpGet("occupancy"),Authorize(Roles=RoleCodes.Admin+","+RoleCodes.Operator+","+RoleCodes.Sales)]
    public async Task<IActionResult> Occupancy([FromQuery]DateOnly? from,[FromQuery]DateOnly? to,[FromQuery]int? tourId,CancellationToken ct){var q=db.TourDepartures.AsNoTracking().Where(x=>x.Status!="Cancelled");if(from.HasValue)q=q.Where(x=>x.DepartureDate>=from.Value);if(to.HasValue)q=q.Where(x=>x.DepartureDate<=to.Value);if(tourId.HasValue)q=q.Where(x=>x.TourId==tourId);var rows=await q.Select(x=>new{x.Id,x.DepartureCode,x.TourId,x.DepartureDate,x.TotalCapacity,x.BookedSlots,OccupancyPercent=x.TotalCapacity==0?0:(decimal)x.BookedSlots*100/x.TotalCapacity}).ToListAsync(ct);return Ok(new{Departures=rows,TotalCapacity=rows.Sum(x=>x.TotalCapacity),BookedSlots=rows.Sum(x=>x.BookedSlots),OccupancyPercent=rows.Sum(x=>x.TotalCapacity)==0?0:(decimal)rows.Sum(x=>x.BookedSlots)*100/rows.Sum(x=>x.TotalCapacity)});}

    [HttpGet("sales-performance"),Authorize(Roles=RoleCodes.Admin+","+RoleCodes.Sales)]
    public async Task<IActionResult> SalesPerformance([FromQuery]DateTime? from,[FromQuery]DateTime? to,CancellationToken ct){var q=DateFilter(db.Bookings.AsNoTracking(),from,to);var result=await q.Where(x=>x.SalesUserId!=null).GroupBy(x=>x.SalesUserId).Select(g=>new{SalesUserId=g.Key,Bookings=g.Count(),Completed=g.Count(x=>x.Status=="Completed"),PaidRevenue=g.Sum(x=>x.PaidAmount),Converted=g.Count(x=>x.PaidAmount>0)}).ToListAsync(ct);return Ok(result.Select(x=>new{x.SalesUserId,x.Bookings,x.Completed,x.PaidRevenue,x.Converted,ConversionPercent=x.Bookings==0?0:(decimal)x.Converted*100/x.Bookings}));}

    private static IQueryable<FinancialLedgers> DateFilter(IQueryable<FinancialLedgers> q,DateTime? from,DateTime? to){if(from.HasValue)q=q.Where(x=>x.OccurredAt>=from.Value);if(to.HasValue)q=q.Where(x=>x.OccurredAt<to.Value.AddDays(1));return q;}
    private static IQueryable<Bookings> DateFilter(IQueryable<Bookings> q,DateTime? from,DateTime? to){if(from.HasValue)q=q.Where(x=>x.CreatedAt>=from.Value);if(to.HasValue)q=q.Where(x=>x.CreatedAt<to.Value.AddDays(1));return q;}
}
public static class ProfitCalculator { public static decimal Calculate(decimal revenue,decimal supplierActualCost,decimal approvedExpenses)=>revenue-supplierActualCost-approvedExpenses; }
