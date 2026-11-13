using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Data;

namespace QuanLyTourDuLich.Api.Workers;

public sealed class BookingFinalPaymentMonitorWorker(IServiceScopeFactory scopes, ILogger<BookingFinalPaymentMonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Final payment monitor pass failed."); }
            try { await timer.WaitForNextTickAsync(stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>(); var notifications = sp.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;
        var due = await db.Bookings.Where(x => x.Status == "DepositPaid" && x.FinalPaymentDueAt != null && x.FinalPaymentDueAt < now).ToListAsync(ct);
        foreach (var booking in due)
        {
            var current = await db.Bookings.SingleOrDefaultAsync(x => x.Id == booking.Id && x.Status == "DepositPaid" && x.FinalPaymentDueAt < now, ct);
            if (current is null) continue;
            current.Status = "FinalPaymentOverdue"; current.UpdatedAt = now;
            try
            {
                await db.SaveChangesAsync(ct);
                var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == current.CustomerId, ct);
                if (customer?.UserId is int userId) await notifications.NotifyUserAsync(userId, "FinalPaymentOverdue", "Khoản thanh toán tour đã quá hạn", $"Booking {current.BookingCode} đã quá hạn thanh toán phần còn lại.", $"/bookings/{current.Id}", ct);
                await notifications.NotifyRoleAsync(RoleCodes.Sales, "FinalPaymentOverdue", "Booking quá hạn thanh toán", $"Booking {current.BookingCode} đã quá hạn thanh toán phần còn lại.", $"/bookings/{current.Id}", ct);
                await notifications.NotifyRoleAsync(RoleCodes.Operator, "FinalPaymentOverdue", "Booking quá hạn thanh toán", $"Booking {current.BookingCode} đã quá hạn thanh toán phần còn lại.", $"/bookings/{current.Id}", ct);
                await notifications.NotifyRoleAsync(RoleCodes.Accountant, "FinalPaymentOverdue", "Booking quá hạn thanh toán", $"Booking {current.BookingCode} đã quá hạn thanh toán phần còn lại.", $"/bookings/{current.Id}", ct);
            }
            catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); }
        }
    }
}
