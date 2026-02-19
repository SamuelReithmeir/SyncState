using System.Reflection;
using SyncState.Enums;
using SyncState.Models.Configuration.EventEmitters;

namespace SyncState.Models.Configuration;

public abstract record PropertyConfiguration : BaseConfiguration
{
    public required List<CommandHandlerConfiguration> CommandHandlerConfigurations { get; init; }
    public required PropertyInfo PropertyInfo { get; init; }
    public required Type PropertyManagerType { get; init; }
}

public record PropertyConfiguration<TProperty> : PropertyConfiguration
{
    public required Func<IServiceProvider, CancellationToken, Task<TProperty>> Gatherer { get; init; }
    public required PropertyGatheringServiceScopeBehavior ScopeBehavior { get; init; }
    public required List<EventEmitterConfiguration<TProperty>> EventEmitters { get; init; }
    public required IEqualityComparer<TProperty> EqualityComparer { get; init; }
}