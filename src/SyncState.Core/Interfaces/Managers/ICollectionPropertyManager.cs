namespace SyncState.Interfaces.Managers;

/// <summary>
/// Interface for a manager that handles a collection property of type <typeparamref name="TEntry"/> on a state object.
/// </summary>
/// <typeparam name="TEntry">The type of entries in the collection.</typeparam>
public interface ICollectionPropertyManager<TEntry> : IPropertyManager<IEnumerable<TEntry>>
{
    /// <summary>
    /// Sets or updates a specific entry in the collection.
    /// </summary>
    /// <param name="entry">The entry to set or update.</param>
    void SetEntry(TEntry entry);
}

/// <summary>
/// Interface for a manager that handles a keyed collection property of type <typeparamref name="TEntry"/> on a state object.
/// </summary>
public interface ICollectionPropertyManager<TEntry, in TKey> : ICollectionPropertyManager<TEntry>
{
    /// <summary>
    /// Removes a specific entry from the collection by its key.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    void RemoveEntry(TKey key);

    /// <summary>
    /// Gets a specific entry from the collection by its key.
    /// </summary>
    /// <param name="key">The key of the entry to retrieve.</param>
    /// <param name="includeUnpublished">If true, returns not yet digested changes when called during a command handling operation.</param>
    /// <returns>The entry with the specified key, or null if not found.</returns>
    TEntry? GetCurrentEntry(TKey key, bool includeUnpublished = false);
}