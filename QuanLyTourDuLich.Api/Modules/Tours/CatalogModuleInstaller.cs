using QuanLyTourDuLich.Api.Common.Modules;

namespace QuanLyTourDuLich.Api.Modules.Tours;

public sealed class CatalogModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TourCatalogService>();
    }
}
