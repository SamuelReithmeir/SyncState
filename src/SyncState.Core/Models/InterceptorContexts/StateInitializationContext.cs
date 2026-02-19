using SyncState.Interfaces.Managers;

namespace SyncState.Models.InterceptorContexts;

public record StateInitializationContext<TState>(IStateManager<TState> StateManager) where TState : class;