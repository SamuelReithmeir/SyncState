using System.Threading.Channels;
using SyncState.Models;

namespace SyncState.Interfaces;

public interface ISyncStateService
{
    /// <summary>
    /// Gets the current state object of type <typeparamref name="TState"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to retrieve.</typeparam>
    /// <returns>A task that represents the asynchronous operation and returns the current state.</returns>
    Task<TState> GetStateAsync<TState>(CancellationToken cancellationToken = default) where TState : class;

    /// <summary>
    /// Gets an async enumerable of state objects of type <typeparamref name="TState"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to stream.</typeparam>
    /// <returns>An async enumerable that streams state objects.</returns>
    IAsyncEnumerable<TState> GetStateEnumerable<TState>(CancellationToken cancellationToken = default)
        where TState : class;

    /// <summary>
    /// Gets a channel reader that streams state objects of type <typeparamref name="TState"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to stream.</typeparam>
    /// <returns>A channel reader that provides access to state objects.</returns>
    ChannelReader<TState> GetStateChannelReader<TState>(CancellationToken cancellationToken = default)
        where TState : class;

    /// <summary>
    /// Registers a callback for state updates of type <typeparamref name="TState"/>.
    /// </summary>
    /// <param name="onStateUpdated">The callback to invoke when the state is updated.</param>
    /// <param name="invokeForCurrentState">If true, the callback is invoked immediately with the current state.</param>
    /// <typeparam name="TState">The type of state to subscribe to.</typeparam>
    void RegisterStateCallback<TState>(Action<TState> onStateUpdated,
        bool invokeForCurrentState = true)
        where TState : class;

    /// <summary>
    /// Gets an async enumerable of events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TEvent">The type of events to stream.</typeparam>
    /// <returns>An async enumerable that streams events.</returns>
    /// <remarks>
    /// Do not use if all events after a specific state version are relevant.
    /// If this is called independently from <see cref="GetStateAsync{TState}"/>, events may be missed due to race conditions.
    /// </remarks>
    IAsyncEnumerable<TEvent> GetEventStreamAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : notnull;

    /// <summary>
    /// Gets an async enumerable of event batches of type <typeparamref name="TEvent"/>.
    /// One batch spans the time between two state emissions (command digestion cycles).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TEvent">The type of events to batch and stream.</typeparam>
    /// <returns>An async enumerable that streams event batches.</returns>
    /// <remarks>
    /// Do not use if all events after a specific state version are relevant.
    /// If this is called independently from <see cref="GetStateAsync{TState}"/>, events may be missed due to race conditions.
    /// </remarks>
    IAsyncEnumerable<EventBatch<TEvent>> GetBatchedEventStreamAsync<TEvent>(
        CancellationToken cancellationToken = default) where TEvent : notnull;

    /// <summary>
    /// Gets the current state of type <typeparamref name="TState"/> and an async enumerable of subsequent events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to retrieve.</typeparam>
    /// <typeparam name="TEvent">The type of events to stream.</typeparam>
    /// <returns>A tuple containing the current state and an async enumerable of subsequent events.</returns>
    /// <remarks>
    /// Guarantees that all events after the provided state version are emitted.
    /// </remarks>
    Task<(TState, IAsyncEnumerable<TEvent>)> GetCurrentStateAndSubsequentEventsAsync<TState, TEvent>(
        CancellationToken cancellationToken = default) where TState : class where TEvent : notnull;

    /// <summary>
    /// Gets the current state of type <typeparamref name="TState"/> and an async enumerable of batched subsequent events of type <typeparamref name="TEvent"/>.
    /// One batch spans the time between two state emissions (command digestion cycles).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to retrieve.</typeparam>
    /// <typeparam name="TEvent">The type of events to batch and stream.</typeparam>
    /// <returns>A tuple containing the current state and an async enumerable of batched subsequent events.</returns>
    /// <remarks>
    /// Guarantees that all events after the provided state version are emitted.
    /// </remarks>
    Task<(TState, IAsyncEnumerable<EventBatch<TEvent>>)> GetCurrentStateAndSubsequentBatchedEventsAsync<TState, TEvent>(
        CancellationToken cancellationToken = default) where TState : class where TEvent : notnull;
}