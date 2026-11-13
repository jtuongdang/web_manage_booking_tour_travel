using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AppDbContext db,
    IPasswordHasher<Users> passwordHasher,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions,
    ICurrentUserService currentUser) : ControllerBase
{
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var phone = request.Phone.Trim();
        var fullName = request.FullName.Trim();
        ValidateRegistration(request, email, phone, fullName);

        if (await db.Users.AnyAsync(u => u.Email == email || u.Phone == phone, cancellationToken))
        {
            throw new ConflictException("An account already exists for this email or phone.");
        }

        var customerRole = await db.Roles.SingleOrDefaultAsync(r => r.Code == RoleCodes.Customer, cancellationToken)
            ?? throw new InvalidOperationException("The CUSTOMER role has not been seeded.");
        var candidates = await db.Customers
            .Where(c => c.Email == email || c.Phone == phone)
            .ToListAsync(cancellationToken);
        var customer = CustomerProfileMatcher.FindMatch(candidates, email, phone);
        if (customer?.UserId is not null)
        {
            throw new ConflictException("The matching customer profile is already linked to an account.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new Users
        {
            Username = await GetUniqueUsernameAsync(request.Username, email, cancellationToken),
            Email = email,
            Phone = phone,
            FullName = fullName,
            UserType = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        if (customer is null)
        {
            customer = new Customers
            {
                CustomerCode = $"CUS-{Guid.NewGuid():N}"[..20],
                FullName = fullName,
                Phone = phone,
                Email = email,
                Source = "Web",
                LoyaltyTier = "Standard",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
        }
        customer.UserId = user.Id;
        db.UserRoles.Add(new UserRoles
        {
            UserId = user.Id,
            RoleId = customerRole.Id,
            AssignedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var auth = await CreateSessionAsync(user, [RoleCodes.Customer], customer.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(auth);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.UsernameOrEmail.Trim();
        var user = await db.Users.SingleOrDefaultAsync(
            u => u.Username == identifier || u.Email == identifier,
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEndAt > now)
        {
            throw new UnauthorizedException("The account is temporarily locked.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.FailedLoginCount = 0;
                user.LockoutEndAt = now.Add(LockoutDuration);
            }
            user.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = now;
        user.UpdatedAt = now;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        var (roles, customerId) = await GetUserRolesAndCustomerAsync(user.Id, cancellationToken);
        var response = await CreateSessionAsync(user, roles, customerId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var oldHash = HashRefreshToken(request.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var oldToken = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == oldHash, cancellationToken);
        var now = DateTime.UtcNow;
        if (oldToken is null || oldToken.RevokedAt is not null || oldToken.ExpiresAt <= now)
        {
            throw new UnauthorizedException("The refresh token is invalid or expired.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == oldToken.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.LockoutEndAt > now)
        {
            throw new UnauthorizedException("The account is inactive or locked.");
        }

        var (roles, customerId) = await GetUserRolesAndCustomerAsync(user.Id, cancellationToken);
        var response = await CreateSessionAsync(user, roles, customerId, cancellationToken);
        var newHash = HashRefreshToken(response.RefreshToken);
        var rows = await db.RefreshTokens
            .Where(t => t.Id == oldToken.Id && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.ReplacedByTokenHash, newHash), cancellationToken);
        if (rows != 1)
        {
            throw new UnauthorizedException("The refresh token has already been used.");
        }

        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
        {
            throw new UnauthorizedException("The authenticated user could not be resolved.");
        }

        var hash = HashRefreshToken(request.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(
            t => t.UserId == userId && t.TokenHash == hash && t.RevokedAt == null,
            cancellationToken);
        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
        {
            throw new UnauthorizedException("The authenticated user could not be resolved.");
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("The account no longer exists.");
        var (roles, customerId) = await GetUserRolesAndCustomerAsync(user.Id, cancellationToken);
        return Ok(new CurrentUserResponse(
            user.Id, user.Username, user.Email, user.Phone, user.FullName,
            user.UserType, customerId, roles));
    }

    private async Task<AuthResponse> CreateSessionAsync(
        Users user,
        IReadOnlyCollection<string> roles,
        int? customerId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshTokens
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = now.AddDays(Math.Clamp(jwtOptions.Value.RefreshTokenDays, 1, 90)),
            CreatedByIp = currentUser.IpAddress,
            UserAgent = Truncate(currentUser.UserAgent, 300),
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(
            jwtTokenService.CreateAccessToken(user, roles, customerId),
            refreshToken,
            now.AddMinutes(Math.Clamp(jwtOptions.Value.AccessTokenMinutes, 1, 60)),
            now.AddDays(Math.Clamp(jwtOptions.Value.RefreshTokenDays, 1, 90)));
    }

    private async Task<(string[] Roles, int? CustomerId)> GetUserRolesAndCustomerAsync(int userId, CancellationToken cancellationToken)
    {
        var roles = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Code).ToArrayAsync(cancellationToken);
        var customerId = await db.Customers.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return (roles, customerId);
    }

    private async Task<string> GetUniqueUsernameAsync(string? supplied, string email, CancellationToken cancellationToken)
    {
        var proposed = string.IsNullOrWhiteSpace(supplied)
            ? email.Split('@')[0]
            : supplied.Trim();
        if (proposed.Length > 60)
        {
            throw new BadRequestException("Username cannot exceed 60 characters.");
        }

        if (!await db.Users.AnyAsync(u => u.Username == proposed, cancellationToken))
        {
            return proposed;
        }

        var username = $"{proposed[..Math.Min(proposed.Length, 48)]}-{Guid.NewGuid():N}"[..Math.Min(proposed.Length + 13, 60)];
        while (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{proposed[..Math.Min(proposed.Length, 48)]}-{Guid.NewGuid():N}"[..Math.Min(proposed.Length + 13, 60)];
        }
        return username;
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;

    private static void ValidateRegistration(RegisterRequest request, string email, string phone, string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120 ||
            string.IsNullOrWhiteSpace(email) || email.Length > 150 || !email.Contains('@') ||
            string.IsNullOrWhiteSpace(phone) || phone.Length > 20 ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new BadRequestException("Full name, valid email, phone, and a password of at least 8 characters are required.");
        }
    }
}

public sealed record RegisterRequest(string FullName, string Email, string Phone, string Password, string? Username = null);
public sealed record LoginRequest(string UsernameOrEmail, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);
public sealed record CurrentUserResponse(int Id, string Username, string Email, string? Phone, string FullName, string UserType, int? CustomerId, IReadOnlyList<string> Roles);
