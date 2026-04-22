namespace SyncState.Interfaces.Managers;

public interface IStateManager<TState> where TState : class
{
    /// <summary>
    /// Sets the value of all property managers based on the values in the new state
    /// </summary>
    /// <param name="newValue"></param>
    void SetValue(TState newValue);

    /// <summary>
    /// Gets the current value of the state, undigested changes are not included
    /// </summary>
    /// <returns></returns>
    TState GetCurrentValue();
}