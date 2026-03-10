using System.Collections.Immutable;
using SyncState.Interfaces.Managers;
using SyncState.InternalInterfaces;
using SyncState.Models.Configuration;

namespace SyncState.Services.Managers;

public class CollectionPropertyManager<TEntry, TKey> : BasePropertyManager<IEnumerable<TEntry>>,
    ICollectionPropertyManager<TEntry, TKey> where TKey : struct
{
    private ImmutableDictionary<TKey, TEntry> _value = ImmutableDictionary<TKey, TEntry>.Empty;
    private readonly CollectionPropertyConfiguration<TEntry, TKey> _configuration;
    private readonly Func<TEntry, TKey> _keySelector;

    private HashSet<TKey> _pendingRemovals = [];
    private Dictionary<TKey, TEntry> _pendingUpserts = [];
    private Dictionary<TKey, TEntry>? _cachedPendingValue;
    private IEnumerable<TEntry>? _cachedMaterializedCollection;

    public CollectionPropertyManager(CollectionPropertyConfiguration<TEntry, TKey> configuration,
        IServiceProvider rootServiceProvider, IInternalSyncEventHub syncEventHub) : base(configuration,
        rootServiceProvider, syncEventHub)
    {
        _configuration = configuration;
        _keySelector = configuration.KeySelector.Compile();
    }

    public void SetEntry(TEntry entry)
    {
        var key = _keySelector(entry);
        if (_pendingRemovals.Remove(key))
        {
            _cachedPendingValue = null; // Invalidate cached pending value
        }

        if (_pendingUpserts.TryGetValue(key, out var existingUpsertEntry) &&
            _configuration.EntryEqualityComparer.Equals(
                existingUpsertEntry, entry) ||
            _value.TryGetValue(key, out var existingEntry) &&
            _configuration.EntryEqualityComparer.Equals(existingEntry, entry))
        {
            return; // No change, skip
        }
        _cachedPendingValue = null; // Invalidate cached pending value
        _pendingUpserts[key] = entry;
    }

    public override void SetValue(IEnumerable<TEntry> newValue)
    {
        _pendingUpserts = newValue.ToDictionary(_keySelector);
        _pendingRemovals = _value.Keys.Except(_pendingUpserts.Keys).ToHashSet();
        _cachedPendingValue = null; // Invalidate cached pending value
    }

    public override IEnumerable<TEntry> GetCurrentValue(bool allowUnpublished = false)
    {
        if (!allowUnpublished || _pendingRemovals.Count == 0 && _pendingUpserts.Count == 0)
        {
            return _value.Values;
        }

        if (_cachedPendingValue != null)
        {
            return _cachedPendingValue.Values;
        }

        var pendingValue = _value
            .Where(kvp => !_pendingRemovals.Contains(kvp.Key)) // Remove entries marked for removal
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Apply upserts
        foreach (var upsert in _pendingUpserts)
        {
            pendingValue[upsert.Key] = upsert.Value;
        }

        _cachedPendingValue = pendingValue; // Cache the computed pending value
        return pendingValue.Values;
    }

    public override void DiscardChanges()
    {
        _pendingRemovals.Clear();
        _pendingUpserts.Clear();
        _cachedPendingValue = null;
    }

    public override void ApplyToStateObject(object stateObject)
    {
        _configuration.PropertyInfo.SetValue(stateObject, GetMaterializedCollection());
    }

    public override Task<bool> CommitChangesAsync(CancellationToken cancellationToken)
    {
        if (_pendingRemovals.Count == 0 && _pendingUpserts.Count == 0)
        {
            return Task.FromResult(false); // No changes to commit
        }

        //emit events 
        foreach (var pendingRemoval in _pendingRemovals.Where(key => _value.ContainsKey(key)))
        {
            foreach (var removeEventEmitterConfiguration in _configuration.OnRemoveEventEmitterConfigurations)
            {
                removeEventEmitterConfiguration.EmitEvent(pendingRemoval, _value[pendingRemoval], SyncEventHub);
            }
        }

        foreach (var pendingUpsert in _pendingUpserts)
        {
            var hasOldValue = _value.TryGetValue(pendingUpsert.Key, out var oldValue);
            var isEqual = _configuration.EntryEqualityComparer.Equals(oldValue, pendingUpsert.Value);
            if (!hasOldValue)
            {
                //added
                foreach (var addEventEmitterConfiguration in _configuration.OnAddEventEmitterConfigurations)
                {
                    addEventEmitterConfiguration.EmitEvent(pendingUpsert.Value, SyncEventHub);
                }
            }
            else if (!isEqual)
            {
                //updated
                foreach (var updateEventEmitterConfiguration in _configuration.OnUpdateEventEmitterConfigurations)
                {
                    updateEventEmitterConfiguration.EmitEvent(pendingUpsert.Value, oldValue!, SyncEventHub);
                }
            }
        }

        _value = _value.RemoveRange(_pendingRemovals);
        _value = _value.SetItems(_pendingUpserts);

        _pendingRemovals.Clear();
        _pendingUpserts.Clear();
        _cachedPendingValue = null;
        _cachedMaterializedCollection = null;

        return Task.FromResult(true);
    }

    public void RemoveEntry(TKey key)
    {
        if (_value.ContainsKey(key) || _pendingUpserts.ContainsKey(key))
        {
            _pendingRemovals.Add(key);
            _pendingUpserts.Remove(key);
            _cachedPendingValue = null; // Invalidate cached pending value
        }
    }

    public TEntry? GetCurrentEntry(TKey key, bool includeUnpublished = false)
    {
        if (!includeUnpublished)
        {
            return CollectionExtensions.GetValueOrDefault(_value, key);
        }

        if (_pendingRemovals.Contains(key))
        {
            return default; // Entry is marked for removal
        }

        if (_pendingUpserts.TryGetValue(key, out var pendingUpsert))
        {
            return pendingUpsert; // Return pending upsert if exists
        }

        return CollectionExtensions.GetValueOrDefault(_value, key); // Return current value
    }

    private IEnumerable<TEntry> GetMaterializedCollection()
    {
        if (_cachedMaterializedCollection != null)
        {
            return _cachedMaterializedCollection;
        }

        var internalView = GetCurrentValue();

        var targetType = _configuration.PropertyInfo.PropertyType;

        // Fast path: already assignable (covers List<T>, HashSet<T>, arrays, etc.)
        if (targetType.IsInstanceOfType(internalView))
        {
            return internalView;
        }

        // Handle arrays
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType();
            if (elementType != typeof(TEntry))
            {
                throw new InvalidOperationException(
                    $"Array element type mismatch. Expected {typeof(TEntry)}, got {elementType}.");
            }

            _cachedMaterializedCollection = internalView.ToArray();
            return _cachedMaterializedCollection;
        }

        // Handle generic collections
        if (targetType.IsGenericType)
        {
            var genericDef = targetType.GetGenericTypeDefinition();

            if (genericDef == typeof(List<>))
            {
                _cachedMaterializedCollection = new List<TEntry>(internalView);
                return _cachedMaterializedCollection;
            }

            if (genericDef == typeof(HashSet<>))
            {
                _cachedMaterializedCollection = new HashSet<TEntry>(internalView);
                return _cachedMaterializedCollection;
            }

            // If property type is an interface (IEnumerable<T>, ICollection<T>, etc.)
            if (targetType.IsAssignableFrom(typeof(List<TEntry>)))
            {
                _cachedMaterializedCollection = new List<TEntry>(internalView);
                return _cachedMaterializedCollection;
            }
        }

        // Last resort: try to construct using IEnumerable<T> ctor
        var ctor = targetType.GetConstructor([typeof(IEnumerable<TEntry>)]);
        if (ctor != null)
        {
            _cachedMaterializedCollection = (IEnumerable<TEntry>)ctor.Invoke([internalView]);
            return _cachedMaterializedCollection;
        }

        throw new InvalidOperationException(
            $"Unsupported collection type '{targetType.FullName}'. " +
            $"Supported: array, List<T>, HashSet<T>, or types with IEnumerable<T> ctor.");
    }
}