namespace SyncState.Models.Configuration;

public record BaseConfiguration
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public required Dictionary<Type, object> Extensions { get; init; }

    public T? GetExtension<T>() where T : class
    {
        return Extensions.TryGetValue(typeof(T), out var value)
            ? (T)value
            : null;
    }
    public bool HasExtension<T>() where T : class => Extensions.ContainsKey(typeof(T));
}