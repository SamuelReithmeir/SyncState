using Microsoft.Extensions.DependencyInjection;

namespace SyncState.Models.Configuration;

public record SyncStateConfiguration : BaseConfiguration
{
    public required List<StateConfiguration> StateConfigurations { get; init; }
    public required List<Action<IServiceCollection>> ServiceCollectionProcessors { get; init; }
    public required List<Func<IServiceProvider, CancellationToken, Task>> InitActions { get; init; }
}