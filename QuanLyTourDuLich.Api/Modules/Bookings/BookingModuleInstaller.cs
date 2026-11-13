using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Modules;
using QuanLyTourDuLich.Api.Workers;

namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public sealed class BookingModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BookingService>();
        services.AddHostedService<BookingExpirationWorker>();
        services.AddHostedService<BookingFinalPaymentMonitorWorker>();
    }
}
