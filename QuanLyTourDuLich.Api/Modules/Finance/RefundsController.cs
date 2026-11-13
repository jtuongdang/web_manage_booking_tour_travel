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
[Route("api/v1/refunds")]
[Authorize]
public sealed class RefundsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IFinancialLedgerWriter ledgerWriter) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = RoleCodes.Accountant + "," + RoleCodes.Admin + "," + RoleCodes.Sales + "," + RoleCodes.Operator)]
    public async Task<ActionResult<IReadOnlyList<RefundRequests>>> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = db.RefundRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = RoleCodes.Customer + "," + RoleCodes.Sales + "," + RoleCodes.Operator + "," + RoleCodes.Admin)]
    public async Task<ActionResult<RefundRequests>> Create(CreateRefundRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy booking.");
        if (currentUser.IsInRole(RoleCodes.Customer) && booking.CustomerId != currentUser.CustomerId)
            throw new ForbiddenException("Bạn không có quyền yêu cầu hoàn tiền cho booking này.");

        var departure = await db.TourDepartures.SingleAsync(x => x.Id == booking.DepartureId, cancellationToken);
        var companyCancelled = departure.Status == "Cancelled";
        if (!companyCancelled && booking.Status != "Cancelled")
            throw new BadRequestException("Chỉ booking đã hủy hoặc đoàn bị công ty hủy mới được yêu cầu hoàn tiền.");
        if (booking.Status == "Refunded") throw new ConflictException("Booking đã được hoàn tiền.");
        if (await db.RefundRequests.AnyAsync(x => x.BookingId == booking.Id && x.Status != "Rejected", cancellationToken))
            throw new ConflictException("Booking đã có yêu cầu hoàn tiền đang xử lý hoặc đã hoàn tất.");

        decimal penalty = 0m;
        if (!companyCancelled)
        {
            try
            {
                penalty = FinanceRules.CalculatePenaltyFromSnapshot(booking, departure.DepartureDate, DateTime.UtcNow);
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new BadRequestException($"CancellationPolicySnapshotJson không hợp lệ: {ex.Message}");
            }
            catch (InvalidOperationException)
            {
                var days = Math.Max(0, departure.DepartureDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber);
                var rule = departure.CancellationPolicyId.HasValue
                    ? await db.CancellationPolicyRules.AsNoTracking()
                        .Where(x => x.PolicyId == departure.CancellationPolicyId.Value
                            && x.MinDaysBeforeDeparture <= days
                            && (!x.MaxDaysBeforeDeparture.HasValue || x.MaxDaysBeforeDeparture.Value >= days))
                        .OrderByDescending(x => x.MinDaysBeforeDeparture)
                        .FirstOrDefaultAsync(cancellationToken)
                    : null;
                if (rule is null) throw new BadRequestException("Không tìm thấy mức phí phạt áp dụng trong snapshot hoặc chính sách hủy.");
                penalty = FinanceRules.CalculatePenalty(booking, rule.PenaltyPercent, companyCancelled: false);
            }
        }
        penalty = Math.Clamp(penalty, 0m, booking.PaidAmount);
        var refund = new RefundRequests
        {
            RefundCode = $"RFD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            BookingId = booking.Id,
            RequestedByUserId = currentUser.UserId,
            Reason = request.Reason,
            PaidAmountSnapshot = booking.PaidAmount,
            PenaltyAmountSnapshot = penalty,
            RefundAmount = FinanceRules.CalculateRefundAmount(booking.PaidAmount, penalty),
            Status = "PendingApproval",
            BankName = request.BankName,
            BankAccountNumber = request.BankAccountNumber,
            BankAccountHolder = request.BankAccountHolder,
            CreatedAt = DateTime.UtcNow
        };
        if (refund.RefundAmount <= 0m) throw new BadRequestException("Số tiền có thể hoàn phải lớn hơn 0.");

        db.RefundRequests.Add(refund);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = refund.Id }, refund);
    }

    [HttpPost("{id:long}/approve")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<ActionResult<RefundRequests>> Approve(long id, CancellationToken cancellationToken)
    {
        var refund = await GetRefund(id, cancellationToken);
        if (refund.Status != "PendingApproval") throw new ConflictException("Chỉ yêu cầu đang chờ duyệt mới được phê duyệt.");
        refund.Status = "Approved";
        refund.ApprovedByUserId = currentUser.UserId;
        refund.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(refund);
    }

    [HttpPost("{id:long}/reject")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<ActionResult<RefundRequests>> Reject(long id, RejectRefundRequest request, CancellationToken cancellationToken)
    {
        var refund = await GetRefund(id, cancellationToken);
        if (refund.Status != "PendingApproval") throw new ConflictException("Chỉ yêu cầu đang chờ duyệt mới được từ chối.");
        refund.Status = "Rejected";
        refund.RejectReason = request.Reason;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(refund);
    }

    [HttpPost("{id:long}/complete")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<ActionResult<RefundRequests>> Complete(long id, CompleteRefundRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BankCode) || string.IsNullOrWhiteSpace(request.TransferRef))
            throw new BadRequestException("BankCode và TransferRef là bắt buộc.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var refund = await GetRefund(id, cancellationToken);
        if (refund.Status != "Approved") throw new ConflictException("Chỉ yêu cầu đã duyệt mới được chi hoàn tiền.");
        var booking = await db.Bookings.SingleAsync(x => x.Id == refund.BookingId, cancellationToken);
        var customer = await db.Customers.SingleAsync(x => x.Id == booking.CustomerId, cancellationToken);
        refund.Status = "Completed";
        refund.CompletedByUserId = currentUser.UserId;
        refund.CompletedAt = DateTime.UtcNow;
        refund.TransferRef = $"{request.BankCode.Trim()}:{request.TransferRef.Trim()}";
        booking.Status = "Refunded";
        booking.UpdatedAt = DateTime.UtcNow;
        customer.TotalSpent = Math.Max(0m, customer.TotalSpent - refund.RefundAmount);
        customer.UpdatedAt = DateTime.UtcNow;

        var invoice = await db.SimulatedInvoices.SingleOrDefaultAsync(x => x.BookingId == booking.Id && x.Status != "Cancelled", cancellationToken);
        if (invoice is not null)
        {
            invoice.Status = "Cancelled";
            invoice.CancelledAt = DateTime.UtcNow;
            invoice.CancelReason = $"Hoàn tiền {refund.RefundCode}";
        }

        await ledgerWriter.AppendAsync(new FinancialLedgerAppendRequest(
            "Out", "CustomerRefund", refund.RefundAmount, $"Hoàn tiền booking {booking.BookingCode}",
            booking.DepartureId, booking.Id, RefundRequestId: refund.Id, RecordedByUserId: currentUser.UserId),
            cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(refund);
    }

    private async Task<RefundRequests> GetRefund(long id, CancellationToken cancellationToken) =>
        await db.RefundRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Không tìm thấy yêu cầu hoàn tiền.");
}

public sealed record CreateRefundRequest(long BookingId, string? Reason, string? BankName, string? BankAccountNumber, string? BankAccountHolder);
public sealed record RejectRefundRequest(string Reason);
public sealed record CompleteRefundRequest(string BankCode, string TransferRef);
