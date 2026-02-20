using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;

namespace SyncState.Models.InterceptorContexts;

public record StateInitializationContext<TState>(
    IStateManager<TState> StateManager,
    StateConfiguration<TState> Configuration) where TState : class;
