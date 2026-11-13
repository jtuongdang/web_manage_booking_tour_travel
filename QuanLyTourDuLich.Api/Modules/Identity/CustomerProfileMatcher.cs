using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

public static class CustomerProfileMatcher
{
    public static Customers? FindMatch(IEnumerable<Customers> profiles, string? email, string? phone)
    {
        var normalizedEmail = email?.Trim();
        var normalizedPhone = phone?.Trim();
        var byEmail = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : profiles.FirstOrDefault(c => string.Equals(c.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));
        var byPhone = string.IsNullOrWhiteSpace(normalizedPhone)
            ? null
            : profiles.FirstOrDefault(c => string.Equals(c.Phone, normalizedPhone, StringComparison.Ordinal));

        if (byEmail is not null && byPhone is not null && byEmail.Id != byPhone.Id)
        {
            throw new ConflictException("The email and phone belong to different customer profiles.");
        }

        return byEmail ?? byPhone;
    }
}
