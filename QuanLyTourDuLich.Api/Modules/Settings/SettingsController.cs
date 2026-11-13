using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Settings;

[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[Route("api/v1/settings")]
public sealed class SettingsController(AppDbContext db, IAuditLogService auditLog, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemSettingResponse>>> List(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking()
            .OrderBy(x => x.SettingKey)
            .Select(x => new SystemSettingResponse(x.SettingKey, x.SettingValue, x.ValueType, x.Description, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<SystemSettingResponse>> Set(
        string key,
        SetSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var settingKey = key.Trim();
        var valueType = request.ValueType.Trim();
        if (settingKey.Length is < 1 or > 80 || request.Value.Length > 500 ||
            request.Description?.Length > 300 ||
            !new[] { "String", "Int", "Decimal", "Bool", "Json" }.Contains(valueType, StringComparer.Ordinal))
        {
            throw new BadRequestException("Invalid system setting key, value, description, or type.");
        }
        ValidateValue(request.Value, valueType);

        var setting = await db.SystemSettings.SingleOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
        var old = setting is null
            ? null
            : new { setting.SettingValue, setting.ValueType, setting.Description };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (setting is null)
        {
            setting = new SystemSettings { SettingKey = settingKey, SettingValue = request.Value, ValueType = valueType };
            db.SystemSettings.Add(setting);
        }
        setting.SettingValue = request.Value;
        setting.ValueType = valueType;
        setting.Description = request.Description;
        setting.UpdatedByUserId = currentUser.UserId;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            "UpdateSystemSetting", "SystemSettings", settingKey,
            oldValue: old,
            newValue: new { setting.SettingValue, setting.ValueType, setting.Description },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new SystemSettingResponse(setting.SettingKey, setting.SettingValue, setting.ValueType, setting.Description, setting.UpdatedAt));
    }

    private static void ValidateValue(string value, string valueType)
    {
        var valid = valueType switch
        {
            "String" => true,
            "Int" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "Decimal" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            "Bool" => bool.TryParse(value, out _),
            "Json" => IsJson(value),
            _ => false
        };
        if (!valid)
        {
            throw new BadRequestException($"The value is not valid for type {valueType}.");
        }
    }

    private static bool IsJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record SetSystemSettingRequest(string Value, string ValueType = "String", string? Description = null);
public sealed record SystemSettingResponse(string SettingKey, string SettingValue, string ValueType, string? Description, DateTime UpdatedAt);
