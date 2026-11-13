using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Operations;

[ApiController, Authorize, Route("api/v1")]
public sealed class OperationsController(AppDbContext db, ICurrentUserService current, IAuditLogService audit, IFinancialLedgerWriter ledger) : ControllerBase
{
    [HttpPut("operations/departures/{id:int}/guides"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Admin)]
    public async Task<IActionResult> AssignGuides(int id, GuideAssignmentRequest r, CancellationToken ct)
    {
        var d=await Departure(id,ct); EnsureNotSettled(d); if(r.Guides.Count==0||r.Guides.Any(x=>x.Role is not ("Lead" or "Assistant"))||r.Guides.Count(x=>x.Role=="Lead")!=1) throw new BadRequestException("Cần đúng một HDV Lead; các role khác phải là Assistant.");
        var guideIds=r.Guides.Select(g=>g.GuideUserId).Distinct().ToArray();
        var activeStaff=await db.Users.Where(x=>guideIds.Contains(x.Id)&&x.UserType=="Staff"&&x.IsActive).Select(x=>x.Id).ToListAsync(ct);
        var assignedGuideRole=await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id where guideIds.Contains(ur.UserId)&&role.Code==RoleCodes.Guide select ur.UserId).Distinct().ToListAsync(ct);
        if(activeStaff.Count!=guideIds.Length||assignedGuideRole.Count!=guideIds.Length) throw new BadRequestException("Tất cả người được phân công phải là nhân viên GUIDE đang hoạt động.");
        if(r.Guides.Select(x=>x.GuideUserId).Distinct().Count()!=r.Guides.Count) throw new BadRequestException("Không được phân công trùng HDV.");
        var old=await db.DepartureGuides.Where(x=>x.DepartureId==id).ToListAsync(ct); db.DepartureGuides.RemoveRange(old);
        foreach(var g in r.Guides) db.DepartureGuides.Add(new DepartureGuides{DepartureId=id,GuideUserId=g.GuideUserId,GuideRole=g.Role,AssignedAt=DateTime.UtcNow,AssignedByUserId=current.UserId,Notes=g.Notes});
        d.VehicleType=r.VehicleType;d.VehiclePlate=r.VehiclePlate;d.DriverName=r.DriverName;d.DriverPhone=r.DriverPhone;
        await db.SaveChangesAsync(ct); await audit.WriteAsync("AssignDepartureGuides","DepartureGuides",id.ToString(),oldValue:old,newValue:r,cancellationToken:ct); return Ok(await db.DepartureGuides.Where(x=>x.DepartureId==id).ToListAsync(ct));
    }

    [HttpGet("operations/departures/{id:int}/checklists"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> Checklist(int id,CancellationToken ct){_=await Departure(id,ct);return Ok(await db.DepartureChecklists.AsNoTracking().Where(x=>x.DepartureId==id).OrderBy(x=>x.SortOrder).ToListAsync(ct));}
    [HttpPost("operations/departures/{id:int}/checklists"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Admin)]
    public async Task<IActionResult> AddChecklist(int id,ChecklistRequest r,CancellationToken ct){var d=await Departure(id,ct);EnsureNotSettled(d);if(string.IsNullOrWhiteSpace(r.TaskName))throw new BadRequestException("TaskName là bắt buộc.");var e=new DepartureChecklists{DepartureId=id,TaskName=r.TaskName.Trim(),SortOrder=r.SortOrder,Notes=r.Notes};db.DepartureChecklists.Add(e);await db.SaveChangesAsync(ct);await Audit("CreateChecklist",e.Id,e,ct);return Created($"/api/v1/operations/departures/{id}/checklists/{e.Id}",e);}
    [HttpPatch("operations/departures/{id:int}/checklists/{checklistId:int}"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> UpdateChecklist(int id,int checklistId,ChecklistUpdateRequest r,CancellationToken ct){var d=await Departure(id,ct);EnsureNotSettled(d);var e=await db.DepartureChecklists.SingleOrDefaultAsync(x=>x.Id==checklistId&&x.DepartureId==id,ct)??throw new NotFoundException("Không tìm thấy checklist.");var old=e.IsCompleted;e.IsCompleted=r.IsCompleted;e.Notes=r.Notes;e.CompletedByUserId=r.IsCompleted?current.UserId:null;e.CompletedAt=r.IsCompleted?DateTime.UtcNow:null;await db.SaveChangesAsync(ct);await audit.WriteAsync("UpdateChecklist","DepartureChecklists",e.Id.ToString(),oldValue:new{IsCompleted=old},newValue:e,cancellationToken:ct);return Ok(e);}

    [HttpPatch("operations/passengers/{id:long}/checkin"), Authorize(Roles=RoleCodes.Guide+","+RoleCodes.Operator+","+RoleCodes.Admin)]
    public async Task<IActionResult> Checkin(long id,CheckinRequest r,CancellationToken ct){if(r.Status is not("Pending" or "CheckedIn" or "Late" or "NoShow"))throw new BadRequestException("CheckInStatus không hợp lệ.");var p=await db.BookingPassengers.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException("Không tìm thấy hành khách.");var b=await db.Bookings.SingleAsync(x=>x.Id==p.BookingId,ct);var d=await Departure(b.DepartureId,ct);EnsureNotSettled(d);var old=p.CheckInStatus;p.CheckInStatus=r.Status;p.CheckedInAt=r.Status=="Pending"?null:DateTime.UtcNow;p.CheckedInByUserId=r.Status=="Pending"?null:current.UserId;await db.SaveChangesAsync(ct);await audit.WriteAsync("PassengerCheckin","BookingPassengers",id.ToString(),oldValue:new{Status=old},newValue:new{Status=p.CheckInStatus},cancellationToken:ct);return Ok(p);}

    [HttpGet("operations/departures/{id:int}/incidents"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> Incidents(int id,CancellationToken ct){_=await Departure(id,ct);return Ok(await db.TourIncidents.AsNoTracking().Where(x=>x.DepartureId==id).OrderByDescending(x=>x.OccurredAt).ToListAsync(ct));}
    [HttpPost("operations/departures/{id:int}/incidents"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> AddIncident(int id,IncidentRequest r,CancellationToken ct){var d=await Departure(id,ct);EnsureNotSettled(d);if(!new[]{"Low","Medium","High","Critical"}.Contains(r.Severity)||string.IsNullOrWhiteSpace(r.Title)||string.IsNullOrWhiteSpace(r.Description))throw new BadRequestException("Mức độ hoặc nội dung sự cố không hợp lệ.");var e=new TourIncidents{DepartureId=id,ReportedByUserId=current.UserId??throw new UnauthorizedException("Không xác định người dùng."),Title=r.Title.Trim(),Severity=r.Severity,Description=r.Description,Status="Open",OccurredAt=r.OccurredAt??DateTime.UtcNow};db.TourIncidents.Add(e);await db.SaveChangesAsync(ct);await Audit("ReportIncident",e.Id,e,ct);return Created($"/api/v1/operations/departures/{id}/incidents/{e.Id}",e);}

    [HttpGet("operations/departures/{id:int}/extra-expenses"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Accountant+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> Expenses(int id,CancellationToken ct){_=await Departure(id,ct);return Ok(await db.DepartureExtraExpenses.AsNoTracking().Where(x=>x.DepartureId==id).OrderByDescending(x=>x.ExpenseDate).ToListAsync(ct));}
    [HttpPost("operations/departures/{id:int}/extra-expenses"), Authorize(Roles=RoleCodes.Operator+","+RoleCodes.Guide+","+RoleCodes.Admin)]
    public async Task<IActionResult> AddExpense(int id,ExpenseRequest r,CancellationToken ct){var d=await Departure(id,ct);EnsureNotSettled(d);if(r.Amount<=0||!new[]{"Meal","Transport","Ticket","Medical","Tip","Other"}.Contains(r.Category)||string.IsNullOrWhiteSpace(r.Title))throw new BadRequestException("Chi phí/category không hợp lệ.");var e=new DepartureExtraExpenses{DepartureId=id,Title=r.Title.Trim(),Category=r.Category,Amount=r.Amount,ExpenseDate=r.ExpenseDate??DateTime.UtcNow,AttachmentUrl=r.AttachmentUrl,Description=r.Description,CreatedByUserId=current.UserId??throw new UnauthorizedException("Không xác định người dùng."),ApprovalStatus="Pending"};db.DepartureExtraExpenses.Add(e);await db.SaveChangesAsync(ct);await Audit("CreateExtraExpense",e.Id,e,ct);return Created($"/api/v1/operations/extra-expenses/{e.Id}",e);}
    [HttpPost("operations/extra-expenses/{id:long}/review"), Authorize(Roles=RoleCodes.Accountant+","+RoleCodes.Admin)]
    public async Task<IActionResult> ReviewExpense(long id,ExpenseReviewRequest r,CancellationToken ct){await using var tx=await db.Database.BeginTransactionAsync(ct);var e=await db.DepartureExtraExpenses.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException("Không tìm thấy chi phí.");var d=await Departure(e.DepartureId,ct);EnsureNotSettled(d);if(e.ApprovalStatus!="Pending")throw new ConflictException("Chỉ duyệt/từ chối chi phí Pending.");if(r.Status is not("Approved" or "Rejected"))throw new BadRequestException("Status phải là Approved hoặc Rejected.");e.ApprovalStatus=r.Status;e.ReviewedByUserId=current.UserId;e.ReviewedAt=DateTime.UtcNow;e.ReviewNote=r.Note;if(r.Status=="Approved")await ledger.AppendAsync(new FinancialLedgerAppendRequest("Out","ExtraExpense",e.Amount,$"Chi phí phát sinh: {e.Title}",e.DepartureId,ExtraExpenseId:e.Id,RecordedByUserId:current.UserId),cancellationToken:ct);await db.SaveChangesAsync(ct);await Audit("ReviewExtraExpense",e.Id,e,ct);await tx.CommitAsync(ct);return Ok(e);}

    [HttpPost("departures/{id:int}/settle"), Authorize(Roles=RoleCodes.Accountant+","+RoleCodes.Admin)]
    public async Task<IActionResult> Settle(int id,CancellationToken ct){await using var tx=await db.Database.BeginTransactionAsync(ct);var d=await Departure(id,ct);if(d.Status=="Settled")throw new ConflictException("Đoàn đã khóa sổ.");if(await db.DepartureExtraExpenses.AnyAsync(x=>x.DepartureId==id&&x.ApprovalStatus=="Pending",ct)||await db.RefundRequests.AnyAsync(x=>x.Status=="PendingApproval"&&db.Bookings.Any(b=>b.Id==x.BookingId&&b.DepartureId==id),ct))throw new ConflictException("Không thể khóa sổ khi còn chi phí Pending hoặc yêu cầu hoàn tiền PendingApproval.");if(d.Status!="Completed")throw new ConflictException("Chỉ đoàn Completed mới được quyết toán.");var old=d.Status;d.Status="Settled";d.SettledAt=DateTime.UtcNow;d.SettledByUserId=current.UserId;try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){throw new ConflictException("Đoàn đã được cập nhật đồng thời; hãy tải lại và thử lại.");}await audit.WriteAsync("SettleDeparture","TourDepartures",id.ToString(),oldValue:new{Status=old},newValue:new{d.Status,d.SettledAt,d.SettledByUserId},cancellationToken:ct);await tx.CommitAsync(ct);return Ok(d);}
    [HttpPost("departures/{id:int}/unlock-settle"), Authorize(Roles=RoleCodes.Admin)]
    public async Task<IActionResult> Unlock(int id,UnlockRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Reason))throw new BadRequestException("Lý do mở khóa là bắt buộc.");var d=await Departure(id,ct);if(d.Status!="Settled")throw new ConflictException("Đoàn chưa khóa sổ.");var old=new{d.Status,d.SettledAt,d.SettledByUserId};d.Status="Completed";d.SettledAt=null;d.SettledByUserId=null;try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){throw new ConflictException("Đoàn đã được cập nhật đồng thời; hãy tải lại và thử lại.");}await audit.WriteAsync("UnlockDepartureSettlement","TourDepartures",id.ToString(),old,new{d.Status,Reason=r.Reason},cancellationToken:ct);return Ok(d);}

    private async Task<TourDepartures> Departure(int id,CancellationToken ct)=>await db.TourDepartures.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException("Không tìm thấy lịch khởi hành.");
    private static void EnsureNotSettled(TourDepartures d)=>SettlementLockRules.EnsureEditable(d.Status);
    private Task Audit(string action,long id,object value,CancellationToken ct)=>audit.WriteAsync(action,"Operations",id.ToString(),newValue:value,cancellationToken:ct);
}
public sealed record GuideItem(int GuideUserId,string Role,string? Notes=null);
public sealed record GuideAssignmentRequest(IReadOnlyList<GuideItem> Guides,string? VehicleType=null,string? VehiclePlate=null,string? DriverName=null,string? DriverPhone=null);
public sealed record ChecklistRequest(string TaskName,int SortOrder=0,string? Notes=null);
public sealed record ChecklistUpdateRequest(bool IsCompleted,string? Notes=null);
public sealed record CheckinRequest(string Status);
public sealed record IncidentRequest(string Title,string Severity,string Description,DateTime? OccurredAt=null);
public sealed record ExpenseRequest(string Title,string Category,decimal Amount,DateTime? ExpenseDate=null,string? AttachmentUrl=null,string? Description=null);
public sealed record ExpenseReviewRequest(string Status,string? Note=null);
public sealed record UnlockRequest(string Reason);
