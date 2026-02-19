using SyncState.Interfaces.Managers;

namespace SyncState.Models.InterceptorContexts;

public record StateCommandContext<TState,TCommand>(TCommand Command, IStateManager<TState> StateManager) where TState : class;