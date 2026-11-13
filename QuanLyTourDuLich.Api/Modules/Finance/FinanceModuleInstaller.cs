using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Modules;

namespace QuanLyTourDuLich.Api.Modules.Finance;

public sealed class FinanceModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITaxInvoiceService, SimulatedTaxInvoiceService>();
    }
}
