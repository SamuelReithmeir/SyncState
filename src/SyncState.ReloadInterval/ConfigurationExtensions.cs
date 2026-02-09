using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;

namespace SyncState.ReloadInterval;

/// <summary>
/// Extension methods for configuring automatic property reloading at specified intervals.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures the property to be automatically reloaded at the specified interval.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="interval">The interval at which to reload the property value. Properties configured with the same interval (value not reference) will be reloaded together to optimize resource usage.</param>
    /// <returns></returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> ReloadEvery<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder, TimeSpan interval)
        where TState : class
    {
        if (builder is not IInternalPropertyConfigurationBuilder<TState, TProperty> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalPropertyConfigurationBuilder.");
        }

        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddOrUpdateExtension(
            new TimerExtension { Intervals = [interval] },
            ext => ext with { Intervals = ext.Intervals.Append(interval).Distinct().ToHashSet() }
        );

        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddServiceCollectionProcessor(services =>
        {
            if (services.Any(sd => sd.ServiceType == typeof(ReloadBackgroundWorker)))
            {
                return;
            }

            services.AddHostedService<ReloadBackgroundWorker>();
        });

        return builder.On<TimedReloadCommand>(e => e.Interval == interval, (_, pm, ct) => pm.ReloadValueAsync(ct));
    }
}