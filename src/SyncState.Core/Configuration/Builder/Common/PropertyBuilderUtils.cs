using SyncState.Commands;
using SyncState.Configuration.Interfaces;

namespace SyncState.Configuration.Builder.Common;

public static class PropertyBuilderUtils
{
    public static void AddDefaultCommandHandlers<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder) where TState : class
    {
        builder.On<ReloadStateCommand<TState>>((_, pm, ct) => pm.ReloadValueAsync(ct));
        builder.On<ReloadAllStatesCommand>((_, pm, ct) => pm.ReloadValueAsync(ct));
    }
}