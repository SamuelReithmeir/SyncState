namespace SyncState.Models.Configuration;

public class BaseConfiguration
{
    public Guid Id { get; }
    
    private readonly Dictionary<Type, object> _extensions;

    public BaseConfiguration(Dictionary<Type, object> extensions)
    {
        _extensions = extensions;
        Id = Guid.NewGuid();
    }

    public T? GetExtension<T>() where T : class
    {
        return _extensions.TryGetValue(typeof(T), out var value)
            ? (T)value
            : null;
    }
    public bool HasExtension<T>() where T : class => _extensions.ContainsKey(typeof(T));
}