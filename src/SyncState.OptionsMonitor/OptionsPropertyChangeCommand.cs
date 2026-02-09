namespace SyncState.OptionsMonitor;

public record OptionsPropertyChangeCommand<TOption>(TOption NewValue);