using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.Identity;
using Xunit;

namespace QuanLyTourDuLich.Tests.Identity;

public sealed class IdentitySecurityTests
{
    private static readonly string SigningSecret = new('k', 32);

    [Fact]
    public void Access_token_signs_and_validates_user_and_all_role_claims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = SigningSecret })
            .Build();
        var provider = new JwtSigningKeyProvider(configuration);
        var service = new JwtTokenService(
            provider,
            Options.Create(new JwtOptions { Issuer = "test-issuer", Audience = "test-audience", AccessTokenMinutes = 20 }));
        var user = new Users { Id = 42, Username = "staff", Email = "staff@example.test", UserType = "Staff" };
        var roles = RoleCodes.All;

        var token = service.CreateAccessToken(user, roles, 501, DateTime.UtcNow.AddMinutes(-1));
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, service.CreateValidationParameters(), out var validated);

        Assert.IsType<JwtSecurityToken>(validated);
        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("staff", principal.Identity?.Name);
        Assert.All(RoleCodes.All, role => Assert.True(principal.IsInRole(role)));
        Assert.Equal("501", principal.FindFirstValue("customer_id"));
        Assert.Equal("staff@example.test", principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void Password_hasher_verifies_correct_password_and_rejects_wrong_password()
    {
        var hasher = new PasswordHasher<Users>();
        var user = new Users { Username = "customer" };
        user.PasswordHash = hasher.HashPassword(user, "Synthetic-Password-1!");

        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, "Synthetic-Password-1!"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, "not-the-password"));
    }

    [Fact]
    public void Customer_profile_matcher_links_by_email_or_phone_and_rejects_ambiguous_identity()
    {
        var byEmail = new Customers { Id = 1, Email = "known@example.test", Phone = "0900000001" };
        var byPhone = new Customers { Id = 2, Email = "other@example.test", Phone = "0900000002" };

        Assert.Same(byEmail, CustomerProfileMatcher.FindMatch([byEmail, byPhone], "KNOWN@example.test", "0900000009"));
        Assert.Same(byPhone, CustomerProfileMatcher.FindMatch([byEmail, byPhone], "new@example.test", "0900000002"));
        Assert.Throws<QuanLyTourDuLich.Api.Common.Exceptions.ConflictException>(
            () => CustomerProfileMatcher.FindMatch([byEmail, byPhone], byEmail.Email, byPhone.Phone));
    }

    [Fact]
    public void Role_catalog_contains_exactly_the_six_supported_rbac_roles()
    {
        Assert.Equal(
            new[] { "CUSTOMER", "SALES", "OPERATOR", "GUIDE", "ACCOUNTANT", "ADMIN" },
            RoleCodes.All);
    }
}
