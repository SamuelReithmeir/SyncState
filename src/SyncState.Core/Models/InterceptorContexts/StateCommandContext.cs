using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;

namespace SyncState.Models.InterceptorContexts;

public record StateCommandContext<TState, TCommand>(
    TCommand Command,
    IStateManager<TState> StateManager,
    StateConfiguration<TState> Configuration) where TState : class;
