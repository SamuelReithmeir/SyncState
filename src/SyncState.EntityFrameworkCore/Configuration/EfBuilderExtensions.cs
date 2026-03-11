using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interception;

namespace SyncState.EntityFrameworkCore.Configuration;

/// <summary>
/// Extension methods for configuring Entity Framework Core integration with SyncState.
/// </summary>
public static class EfBuilderExtensions
{
    /// <summary>
    /// Configures a collection property to use Entity Framework Core as the data provider for updates using interceptors
    /// </summary>
    /// <returns>A partial EF Core collection builder for further configuration.</returns>
    public static IPartialEfCoreCollectionBuilder<TState, TEntry, TKey> WithEfCoreProvider<TState, TEntry, TKey>(
        this ICollectionPropertyBuilder<TState, TEntry, TKey> builder) where TState : class where TKey : struct
    {
        return new PartialEfCoreCollectionBuilder<TState, TEntry, TKey>(builder);
    }

    /// <summary>
    /// Enables Entity Framework Core integration for automatic state synchronization on database changes.
    /// </summary>
    /// <param name="builder">The SyncState builder.</param>
    /// <param name="waitForTransactionCompletion">If true, waits for the database transaction to complete before dispatching state updates.</param>
    /// <typeparam name="TDbContext">The type of DbContext to intercept.</typeparam>
    /// <returns>The SyncState builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the builder doesn't implement the required internal interface.</exception>
    public static ISyncStateBuilder EnableEfCoreProvider<TDbContext>(this ISyncStateBuilder builder,
        bool waitForTransactionCompletion = true) where TDbContext : DbContext
    {
        if (builder is not IInternalSyncStateBuilder internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalSyncStateBuilder");
        }

        internalBuilder.AddOrUpdateExtension(new EfCoreSyncStateExtension
            {
                WaitForTransactionCommit = waitForTransactionCompletion,
                ConfiguredDbContextTypes = [typeof(TDbContext)]
            }, ext => ext with
            {
                WaitForTransactionCommit = waitForTransactionCompletion,
                ConfiguredDbContextTypes = [..ext.ConfiguredDbContextTypes, typeof(TDbContext)]
            }
        );

        internalBuilder.AddServiceCollectionProcessor(sc =>
        {
            sc.AddScoped<SyncStateDbContextInterceptor>();
        });
        return builder;
    }
}