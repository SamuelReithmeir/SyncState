namespace SyncState.Models.Diagnostics;

public abstract record StateChangeData;
public record StateChangeData<TState>(TState OldState, TState NewState) : StateChangeData;