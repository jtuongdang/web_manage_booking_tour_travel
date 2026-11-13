using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Modules;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

public sealed class IdentitySeedInitializer(
    IConfiguration configuration,
    IPasswordHasher<Users> passwordHasher) : IStartupInitializer
{
    private static readonly (string Code, string Name, string Email, string FullName, string UserType)[] SeedUsers =
    [
        (RoleCodes.Admin, "Quản trị viên", "admin@viettour.local", "Quản trị viên mẫu", "Staff"),
        (RoleCodes.Sales, "Kinh doanh", "sales@viettour.local", "Nhân viên kinh doanh mẫu", "Staff"),
        (RoleCodes.Operator, "Điều hành", "operator@viettour.local", "Điều hành mẫu", "Staff"),
        (RoleCodes.Guide, "Hướng dẫn viên", "guide@viettour.local", "Hướng dẫn viên mẫu", "Staff"),
        (RoleCodes.Accountant, "Kế toán", "accountant@viettour.local", "Kế toán mẫu", "Staff"),
        (RoleCodes.Customer, "Khách hàng", "customer@viettour.local", "Khách hàng mẫu", "Customer")
    ];

    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roles = await db.Roles.ToDictionaryAsync(r => r.Code, cancellationToken);
        foreach (var code in RoleCodes.All)
        {
            if (!roles.ContainsKey(code))
            {
                var role = new Roles
                {
                    Code = code,
                    Name = SeedUsers.First(x => x.Code == code).Name,
                    Description = $"Vai trò {code}"
                };
                db.Roles.Add(role);
                roles.Add(code, role);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var seed in SeedUsers)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == seed.Code.ToLowerInvariant(), cancellationToken);
            if (user is null)
            {
                user = new Users
                {
                    Username = seed.Code.ToLowerInvariant(),
                    Email = seed.Email,
                    FullName = seed.FullName,
                    UserType = seed.UserType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                user.PasswordHash = passwordHasher.HashPassword(user, GetSeedPassword(seed.Code));
                db.Users.Add(user);
                await db.SaveChangesAsync(cancellationToken);
            }

            if (!await db.UserRoles.AnyAsync(
                    ur => ur.UserId == user.Id && ur.RoleId == roles[seed.Code].Id,
                    cancellationToken))
            {
                db.UserRoles.Add(new UserRoles
                {
                    UserId = user.Id,
                    RoleId = roles[seed.Code].Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            if (seed.Code == RoleCodes.Customer)
            {
                var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
                if (customer is null)
                {
                    customer = await db.Customers.SingleOrDefaultAsync(
                        c => c.Email == seed.Email || c.Phone == "0900000006",
                        cancellationToken);
                    if (customer is null)
                    {
                        customer = new Customers
                        {
                            CustomerCode = $"CUS-SEED-{Guid.NewGuid():N}"[..20],
                            FullName = user.FullName,
                            Phone = "0900000006",
                            Email = seed.Email,
                            Source = "Web",
                            LoyaltyTier = "Standard",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.Customers.Add(customer);
                    }

                    if (customer.UserId is not null && customer.UserId != user.Id)
                    {
                        throw new InvalidOperationException("The seeded customer profile is already linked to another user.");
                    }

                    customer.UserId = user.Id;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private string GetSeedPassword(string username) =>
        configuration[$"Identity:SeedPasswords:{username.ToLowerInvariant()}"]
        ?? configuration["Identity:SeedPassword"]
        ?? "VietTour@2026!";
}
