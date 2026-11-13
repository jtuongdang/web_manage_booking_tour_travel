using QuanLyTourDuLich.Api.Common.Modules;

namespace QuanLyTourDuLich.Api.Modules.Departures;

public sealed class DepartureModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration) => services.AddScoped<DepartureService>();
}
