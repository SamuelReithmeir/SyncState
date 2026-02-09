namespace SyncState.Sample.Events;

public record ActiveUserCountChangedEvent(int Count):ApplicationStateEvent;