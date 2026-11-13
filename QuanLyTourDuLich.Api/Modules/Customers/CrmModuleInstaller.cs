using QuanLyTourDuLich.Api.Common.Modules;
using QuanLyTourDuLich.Api.Modules.Promotions;

namespace QuanLyTourDuLich.Api.Modules.Crm;

public sealed class CrmModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPromotionValidationService, PromotionValidationService>();
    }
}
