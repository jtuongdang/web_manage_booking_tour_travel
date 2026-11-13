using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

public sealed class JwtTokenService(
    IJwtSigningKeyProvider signingKeyProvider,
    IOptions<JwtOptions> options)
{
    public string CreateAccessToken(Users user, IReadOnlyCollection<string> roles, int? customerId, DateTime? issuedAt = null)
    {
        var now = issuedAt ?? DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("user_type", user.UserType)
        };
        if (customerId.HasValue)
        {
            claims.Add(new Claim("customer_id", customerId.Value.ToString()));
        }

        claims.AddRange(roles.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => new Claim(ClaimTypes.Role, role)));

        var jwtOptions = options.Value;
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(Math.Clamp(jwtOptions.AccessTokenMinutes, 1, 60)),
            signingCredentials: new SigningCredentials(signingKeyProvider.GetSigningKey(), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Value.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Value.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKeyProvider.GetSigningKey(),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name,
        ClockSkew = TimeSpan.Zero
    };
}
