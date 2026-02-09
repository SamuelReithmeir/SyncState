using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Builder;
using SyncState.Configuration.Interfaces;
using SyncState.Factories;
using SyncState.Interfaces;
using SyncState.InternalInterfaces;
using SyncState.Services;

namespace SyncState.Configuration;

/// <summary>
/// Extension methods for registering SyncState services in the dependency injection container.
/// </summary>
public static class SyncStateExtensions
{
    /// <summary>
    /// Adds SyncState services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Configuration action for setting up states and their properties.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSyncState(this IServiceCollection services, Action<ISyncStateBuilder> configure)
    {
        var builder = new SyncStateBuilder();
        configure(builder);
        var syncStateConfiguration = builder.Build();

        services.AddSingleton(syncStateConfiguration);
        services.AddSingleton<IInternalSyncStateService, SyncStateService>();
        services.AddSingleton<ISyncStateService>(sp => sp.GetRequiredService<IInternalSyncStateService>());
        services.AddSingleton<IInternalSyncEventHub, SyncEventHub>();
        services.AddHostedService<SyncStateInitializer>();
        services.AddScoped<ISyncCommandService, SyncCommandService>();
        if (services.All(x => x.ServiceType != typeof(IStateManagerFactory)))
        {
            services.AddSingleton<IStateManagerFactory, DefaultStateManagerFactory>();
        }

        if (services.All(x => x.ServiceType != typeof(IPropertyManagerFactory)))
        {
            services.AddSingleton<IPropertyManagerFactory, DefaultPropertyManagerFactory>();
        }

        foreach (var serviceCollectionProcessor in syncStateConfiguration.ServiceCollectionProcessors)
        {
            serviceCollectionProcessor(services);
        }

        return services;
    }
}