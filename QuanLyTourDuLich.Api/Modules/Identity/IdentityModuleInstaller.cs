using Microsoft.AspNetCore.Identity;
using QuanLyTourDuLich.Api.Common.Modules;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Identity;

public sealed class IdentityModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher<Users>, PasswordHasher<Users>>();
        services.AddSingleton<JwtTokenService>();
    }
}
