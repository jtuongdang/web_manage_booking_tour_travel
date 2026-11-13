using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Data;

namespace QuanLyTourDuLich.Api.Workers;

public sealed class BookingExpirationWorker(IServiceScopeFactory scopes, ILogger<BookingExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(45));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ExpireAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Booking expiration pass failed."); }
            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expired = await db.Bookings.Where(x => x.Status == "PendingPayment" && x.HoldExpiresAt < DateTime.UtcNow).OrderBy(x => x.HoldExpiresAt).Take(100).ToListAsync(ct);
        foreach (var booking in expired)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var current = await db.Bookings.SingleOrDefaultAsync(x => x.Id == booking.Id && x.Status == "PendingPayment" && x.HoldExpiresAt < DateTime.UtcNow, ct);
            if (current is null) { await tx.RollbackAsync(ct); continue; }
            var departure = await db.TourDepartures.SingleAsync(x => x.Id == current.DepartureId, ct);
            departure.HeldSlots = Math.Max(0, departure.HeldSlots - current.SeatsReserved);
            departure.AvailableSlots = departure.TotalCapacity - departure.HeldSlots - departure.BookedSlots;
            if (departure.Status == "Full" && departure.AvailableSlots > 0) departure.Status = "Open";
            current.Status = "Expired"; current.UpdatedAt = DateTime.UtcNow;
            var usage = await db.PromotionUsages.SingleOrDefaultAsync(x => x.BookingId == current.Id && x.Status == "Reserved", ct);
            if (usage is not null) { usage.Status = "Released"; usage.ReleasedAt = DateTime.UtcNow; var promo = await db.Promotions.SingleAsync(x => x.Id == usage.PromotionId, ct); promo.UsedCount = Math.Max(0, promo.UsedCount - 1); }
            try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch (DbUpdateConcurrencyException) { await tx.RollbackAsync(ct); db.ChangeTracker.Clear(); }
        }
    }
}
