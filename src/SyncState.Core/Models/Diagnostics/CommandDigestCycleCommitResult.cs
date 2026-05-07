namespace SyncState.Models.Diagnostics;

public record CommandDigestCycleCommitResult(List<StateChangeData> StateChanges)
{
    /// <summary>
    /// gets the state change data for the specified state type, returns null if there is no state change data for the specified state type
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    public StateChangeData<TState>? TryGetStateChangeData<TState>()
    {
        var stateChangeData = StateChanges.OfType<StateChangeData<TState>>().FirstOrDefault();
        return stateChangeData;
    }
}