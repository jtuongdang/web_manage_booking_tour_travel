using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[Route("api/v1/users")]
public sealed class UsersController(
    AppDbContext db,
    IPasswordHasher<Users> passwordHasher,
    IAuditLogService auditLog,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItem>>> List(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search) || u.FullName.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(u => u.Id)
            .Skip((request.NormalizedPageIndex - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var userIds = items.Select(user => user.Id).ToArray();
        var roles = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Code }).ToListAsync(cancellationToken);
        var roleMap = roles.GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(item => item.Code).ToArray());
        var results = items.Select(user => new UserListItem(
            user.Id,
            user.Username,
            user.Email,
            user.Phone,
            user.FullName,
            user.UserType,
            user.IsActive,
            user.LockoutEndAt,
            roleMap.GetValueOrDefault(user.Id, []))).ToArray();

        return Ok(new PagedResult<UserListItem>(results, request.NormalizedPageIndex, request.NormalizedPageSize, total));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<UserListItem>> CreateStaff(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var roleCode = request.RoleCode.Trim().ToUpperInvariant();
        if (!RoleCodes.InternalStaff.Contains(roleCode, StringComparer.Ordinal))
        {
            throw new BadRequestException("Staff accounts require an internal staff role.");
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var fullName = request.FullName.Trim();
        if (username.Length is < 1 or > 60 || email.Length is < 1 or > 150 ||
            fullName.Length is < 1 or > 120 || request.Password.Length < 8 ||
            (phone?.Length > 20))
        {
            throw new BadRequestException("Invalid staff account fields.");
        }
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == email || (phone != null && u.Phone == phone), cancellationToken))
        {
            throw new ConflictException("An account already exists for the username, email, or phone.");
        }

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Code == roleCode, cancellationToken)
            ?? throw new BadRequestException("The requested role does not exist.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new Users
        {
            Username = username,
            Email = email,
            Phone = phone,
            FullName = fullName,
            UserType = "Staff",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        db.UserRoles.Add(new UserRoles
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedByUserId = currentUser.UserId,
            AssignedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            "CreateStaff", "Users", user.Id.ToString(),
            newValue: new { user.Username, user.Email, Role = role.Code },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(nameof(List), new { id = user.Id },
            new UserListItem(user.Id, username, email, phone, fullName, user.UserType, user.IsActive, null, [role.Code]));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, SetUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        if (currentUser.UserId == id && !request.IsActive)
        {
            throw new ConflictException("You cannot deactivate your own account.");
        }

        var old = user.IsActive;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            request.IsActive ? "ActivateUser" : "DeactivateUser",
            "Users",
            user.Id.ToString(),
            oldValue: new { IsActive = old },
            newValue: new { IsActive = user.IsActive },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, SetUserRoleRequest request, CancellationToken cancellationToken)
    {
        var roleCode = request.RoleCode.Trim().ToUpperInvariant();
        if (!RoleCodes.InternalStaff.Contains(roleCode, StringComparer.Ordinal))
        {
            throw new BadRequestException("The requested role is not an internal staff role.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        if (user.UserType != "Staff")
        {
            throw new ConflictException("Customer account roles cannot be changed through the staff role endpoint.");
        }

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Code == roleCode, cancellationToken)
            ?? throw new BadRequestException("The requested role does not exist.");
        var oldRoles = await (
            from userRole in db.UserRoles
            join existingRole in db.Roles on userRole.RoleId equals existingRole.Id
            where userRole.UserId == id
            select new { UserRole = userRole, existingRole.Code }).ToListAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.UserRoles.RemoveRange(oldRoles.Select(x => x.UserRole));
        db.UserRoles.Add(new UserRoles
        {
            UserId = id,
            RoleId = role.Id,
            AssignedByUserId = currentUser.UserId,
            AssignedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            "ChangeUserRole", "Users", id.ToString(),
            oldValue: oldRoles.Select(x => x.Code).ToArray(),
            newValue: new[] { role.Code },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record CreateStaffRequest(string Username, string Email, string? Phone, string FullName, string Password, string RoleCode);
public sealed record SetUserStatusRequest(bool IsActive);
public sealed record SetUserRoleRequest(string RoleCode);
public sealed record UserListItem(int Id, string Username, string Email, string? Phone, string FullName, string UserType, bool IsActive, DateTime? LockoutEndAt, IReadOnlyList<string> Roles);
