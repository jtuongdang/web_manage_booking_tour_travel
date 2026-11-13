using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Common.Services;

public sealed class AuditLogService(AppDbContext db, ICurrentUserService currentUser) : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(
        string action,
        string entityName,
        string entityId,
        object? oldValue = null,
        object? newValue = null,
        int? actorUserIdOverride = null,
        string? actorRoleOverride = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLogs
        {
            ActorUserId = actorUserIdOverride ?? currentUser.UserId,
            ActorRole = actorRoleOverride ?? currentUser.Roles.FirstOrDefault(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
            NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SystemSettingService(AppDbContext db) : ISystemSettingService
{
    public async Task<string> GetStringAsync(string key, string fallback, CancellationToken cancellationToken = default)
    {
        var raw = await db.SystemSettings
            .AsNoTracking()
            .Where(s => s.SettingKey == key)
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    public async Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default)
    {
        var raw = await GetStringAsync(key, string.Empty, cancellationToken);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken = default)
    {
        var raw = await GetStringAsync(key, string.Empty, cancellationToken);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default)
    {
        var raw = await GetStringAsync(key, string.Empty, cancellationToken);
        return bool.TryParse(raw, out var parsed) ? parsed : fallback;
    }
}

public sealed class NotificationService(AppDbContext db) : INotificationService
{
    public async Task NotifyUserAsync(
        int userId,
        string type,
        string title,
        string body,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        db.Notifications.Add(new Notifications
        {
            UserId = userId,
            TargetRole = null,
            Type = type,
            Title = title,
            Body = body,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyRoleAsync(
        string targetRole,
        string type,
        string title,
        string body,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        db.Notifications.Add(new Notifications
        {
            UserId = null,
            TargetRole = targetRole,
            Type = type,
            Title = title,
            Body = body,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class FinancialLedgerWriter(AppDbContext db, ICurrentUserService currentUser) : IFinancialLedgerWriter
{
    public async Task AppendAsync(
        FinancialLedgerAppendRequest request,
        bool saveChanges = false,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) return;

        var now = DateTime.UtcNow;
        var entry = new FinancialLedgers
        {
            EntryCode = $"LDG-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Direction = request.Direction,
            Category = request.Category,
            Amount = request.Amount,
            DepartureId = request.DepartureId,
            BookingId = request.BookingId,
            PaymentTransactionId = request.PaymentTransactionId,
            RefundRequestId = request.RefundRequestId,
            SupplierPaymentId = request.SupplierPaymentId,
            ExtraExpenseId = request.ExtraExpenseId,
            Description = request.Description,
            OccurredAt = request.OccurredAt ?? now,
            RecordedByUserId = request.RecordedByUserId ?? currentUser.UserId,
            CreatedAt = now
        };
        db.FinancialLedgers.Add(entry);
        if (saveChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
