using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Modules;

namespace QuanLyTourDuLich.Api.Modules.Payments;

public sealed class PaymentsModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentGateway, VietQrPaymentGateway>();
    }
}
