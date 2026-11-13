using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace QuanLyTourDuLich.Api.Common.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "QuanLyTourDuLich.Api";
    public string Audience { get; set; } = "QuanLyTourDuLich.Clients";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
    public string? SigningKey { get; set; }
}

public interface IJwtSigningKeyProvider
{
    SymmetricSecurityKey GetSigningKey();
}

public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private readonly SymmetricSecurityKey _key;

    public JwtSigningKeyProvider(IConfiguration configuration)
    {
        var rawKey = configuration["Jwt:SigningKey"];
        if (!string.IsNullOrWhiteSpace(rawKey) && Encoding.UTF8.GetByteCount(rawKey) >= 32)
        {
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
        }
        else
        {
            // Runtime-generated fallback key ensures dev/test environments work without committing secrets in repo files.
            _key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        }
    }

    public SymmetricSecurityKey GetSigningKey() => _key;
}

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    string? UserType { get; }
    int? CustomerId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("username");
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? UserType => User?.FindFirstValue("user_type");

    public int? CustomerId
    {
        get
        {
            var raw = User?.FindFirstValue("customer_id");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList() ?? [];

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
