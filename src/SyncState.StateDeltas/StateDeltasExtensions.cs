using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Models.Configuration.Extensions;

namespace SyncState.StateDeltas;

/// <summary>
/// Extension methods for configuring state delta functionality in SyncState.
/// </summary>
public static class StateDeltasExtensions
{
    /// <summary>
    /// Enables state delta encoding for efficient state synchronization using JSON patches.
    /// </summary>
    /// <param name="builder">The SyncState builder.</param>
    /// <param name="jsonOptions">Optional JSON serialization options for state serialization.</param>
    /// <returns>The SyncState builder for method chaining.</returns>
    public static ISyncStateBuilder EnableStateDeltas(this ISyncStateBuilder builder,
        JsonSerializerOptions? jsonOptions = null)
    {
        if (builder is not IInternalSyncStateBuilder internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalSyncStateBuilder");
        }

        if (jsonOptions != null)
        {
            internalBuilder.AddExtension(new JsonSerializationExtension
                { JsonSerializerOptions = jsonOptions });
        }

        internalBuilder.AddServiceCollectionProcessor(services =>
        {
            services.TryAddSingleton<ISyncStateDeltaProvider, SyncStateDeltaProvider>();
        });
        return builder;
    }
}