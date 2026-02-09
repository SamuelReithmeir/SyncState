namespace SyncState.Interfaces.Managers;

/// <summary>
/// Public interface for a manager that handles a single property on a state object.
/// </summary>
public interface IPropertyManager
{
    /// <summary>
    /// Reloads the property value from the configured gatherer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReloadValueAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Public interface for a manager that handles a single property of type <typeparamref name="TProperty"/> on a state object.
/// </summary>
/// <typeparam name="TProperty">The type of the property being managed.</typeparam>
public interface IPropertyManager<TProperty> : IPropertyManager
{
    /// <summary>
    /// Sets the property value.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    void SetValue(TProperty newValue);
    
    /// <summary>
    /// Gets the current value of the property.
    /// </summary>
    /// <param name="allowUnpublished">If true, returns not yet digested changes when called during a command handling operation.</param>
    /// <returns>The current value of the property.</returns>
    TProperty GetCurrentValue(bool allowUnpublished = false);
}