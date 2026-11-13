using System.Reflection;

namespace QuanLyTourDuLich.Api.Common.Modules;

/// <summary>
/// Allows each feature module in Modules/** to register its DI services and background workers
/// without modifying Program.cs.
/// </summary>
public interface IModuleInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}

/// <summary>
/// Executed once on application startup after DI container build (e.g., idempotent RBAC/account seeding).
/// </summary>
public interface IStartupInitializer
{
    int Order => 0;
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken);
}

public static class ModuleDiscoveryExtensions
{
    public static IServiceCollection AddApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var scanAssemblies = assemblies.Length > 0 ? assemblies : [typeof(ModuleDiscoveryExtensions).Assembly];

        var installerTypes = scanAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModuleInstaller).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var type in installerTypes)
        {
            if (Activator.CreateInstance(type) is IModuleInstaller installer)
            {
                installer.Install(services, configuration);
            }
        }

        var initTypes = scanAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IStartupInitializer).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var type in initTypes)
        {
            services.AddScoped(typeof(IStartupInitializer), type);
        }

        return services;
    }

    public static async Task RunStartupInitializersAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("StartupInitializers");
        var initializers = scope.ServiceProvider
            .GetServices<IStartupInitializer>()
            .OrderBy(x => x.Order);

        foreach (var initializer in initializers)
        {
            try
            {
                await initializer.InitializeAsync(scope.ServiceProvider, cancellationToken);
            }
            catch (Exception ex) when (app.Environment.IsEnvironment("Testing") || ex.GetType().Name.Contains("SqlException", StringComparison.Ordinal))
            {
                logger?.LogWarning(ex, "Skipping startup initializer {Initializer} because database is unavailable.", initializer.GetType().Name);
            }
        }
    }
}
