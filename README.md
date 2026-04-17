# SyncState

SyncState is an ASP.NET library for defining server-side application state and synchronizing it to clients in real time.

Using a fluent builder API you declare **what** your state looks like, **where** each piece of data comes from, and **what triggers it to change** — SyncState handles the rest: gathering initial values, listening for commands, digesting changes into a consistent new state, and broadcasting that state (or a diff of it) to all subscribers.

---

## Packages

| Package | Description |
|---|---|
| `SyncState.Core` | Core engine: fluent configuration, state/property managers, command handling, event streaming |
| `SyncState.SignalR` | Base `Hub` class for streaming state and delta-encoded patches to SignalR clients |
| `SyncState.StateDeltas` | JSON-patch delta encoding — send only what changed instead of the full state |
| `SyncState.EntityFrameworkCore` | EF Core interceptor that automatically triggers state updates on `SaveChanges` |
| `SyncState.ErrorHandling` | Retry and fallback interceptors for resilient property loading |
| `SyncState.ReloadInterval` | Background worker that reloads configured properties on a fixed timer |
| `SyncState.OptionsMonitor` | Automatically re-syncs a property whenever `IOptionsMonitor<T>` reports a config change |

---

## Getting Started

### 1. Install packages

```shell
dotnet add package SyncState.Core
# plus whichever add-ons you need, e.g.:
dotnet add package SyncState.SignalR
dotnet add package SyncState.EntityFrameworkCore
```

### 2. Define your state class

A state is a plain C# class. Every public property **must** be configured.

```csharp
public class ApplicationStateDto
{
    public int ActiveUserCount { get; set; }
    public List<OrderDto> Orders { get; set; }
}
```

### 3. Register SyncState in `Program.cs`

```csharp
builder.Services.AddSyncState(config =>
{
    config.AddState<ApplicationStateDto>(state =>
    {
        // Simple scalar property — gathered from a singleton service
        state.Property(x => x.ActiveUserCount)
            .GatherFrom<IActiveUserStore>(s => s.GetUsers().Count);

        // Collection property with a key selector for efficient change tracking
        state.Collection(x => x.Orders)
            .WithKey(x => x.Id)
            .GatherFromAsync<AppDbContext>((db, ct) =>
                db.Orders.Select(o => o.ToDto()).ToListAsync(ct));
    });
});
```

### 4. Read state anywhere

Inject `ISyncStateService` to get the current state or subscribe to a live stream:

```csharp
// one-shot read
var state = await syncStateService.GetStateAsync<ApplicationStateDto>();

// live stream — yields a new value every time state changes
await foreach (var state in syncStateService.GetStateEnumerable<ApplicationStateDto>(ct))
{
    // push to client, log, etc.
}
```

### 5. Trigger state changes via commands

Inject the **scoped** `ISyncCommandService` to push changes:

```csharp
// Define a command (any class or record)
public record OrderCreatedCommand(OrderDto Order);

// Wire it up during configuration
state.Collection(x => x.Orders)
    .WithKey(x => x.Id)
    // ...
    .On<OrderCreatedCommand>((cmd, manager) => manager.SetEntry(cmd.Order));

// Dispatch from a controller / service
await commandService.HandleAsync(new OrderCreatedCommand(newOrder));
```

SyncState batches all property changes triggered by a single `HandleAsync` call into one **digest cycle**, emits a single new state snapshot, and then broadcasts any configured events — all under a lock, so subscribers always see a consistent state.

---

## Key Concepts

| Concept | Description |
|---|---|
| **State** | A plain C# class whose properties represent a slice of application state |
| **Property / Collection** | A single value or keyed collection on a state, with its own gatherer and command handlers |
| **Property manager** | An `IPropertyManager<TProperty>` instance created per configured property at startup. Holds the committed value; command handlers receive it as a parameter to read, mutate, or reload the property |
| **State manager** | An `IStateManager<TState>` instance that owns all property managers for a state and reconstructs the full state object after every digest cycle commit |
| **Gatherer** | A function (`GatherFrom` / `GatherFromAsync`) that loads the initial (or refreshed) value of a property from the DI container |
| **Command** | Any object dispatched via `ISyncCommandService.HandleAsync`. Handlers on properties react to commands to mutate state |
| **Digest cycle** | The unit of work that groups one or more command handlings, collects all resulting property changes, emits a new state snapshot, and publishes queued events atomically |
| **Event** | A typed object emitted when a property value changes, configured with `.Emit<TEvent>(...)` on the property builder |
| **Interceptor** | A middleware hook (`IStateInterceptor<TState>` / `IPropertyInterceptor<TProperty>`) called around initialization and command handling |

---

## Core Concepts In Depth

### Properties

Every public property on a state class must be configured. Use `state.Property(...)` for scalar values:

```csharp
state.Property(x => x.ActiveUserCount)
    .GatherFrom<IActiveUserStore>(s => s.GetUsers().Count);
```

> **All properties must be configured.** Leaving any public property on a state class unconfigured is an error caught at startup.

Command handlers receive this property's **property manager** (`IPropertyManager<TProperty>`) as their second parameter, giving access to `SetValue`, `GetCurrentValue`, and `ReloadValueAsync`. For the full API and lifecycle details see [Property & State Managers](#property--state-managers).

#### Gatherers

A gatherer is a function that produces the initial value of a property (and re-produces it whenever a `ReloadValueAsync` is requested). It receives a service resolved from the DI container.

| Method | When to use |
|---|---|
| `GatherFrom<TService>(Func<TService, TValue>)` | Synchronous retrieval |
| `GatherFromAsync<TService>(Func<TService, Task<TValue>>)` | Async retrieval |
| `GatherFromAsync<TService>(Func<TService, CancellationToken, Task<TValue>>)` | Async + cancellation |

The service is resolved according to the **scope behavior** (see below).

#### Scope behavior

Controls how the DI scope is managed when the gatherer runs:

| `PropertyGatheringServiceScopeBehavior` | Behavior |
|---|---|
| `ShareScope` *(default)* | Re-uses the scope of the current digest cycle |
| `CreateOwnScope` | Creates a fresh `IServiceScope` for each reload |
| `ResolveFromRoot` | Resolves the service directly from the root container |

```csharp
state.Property(x => x.SomeValue)
    .GatherFromAsync<MyDbContext>(...)
    .ScopeBehavior(PropertyGatheringServiceScopeBehavior.CreateOwnScope);
```

---

### Collections

Use `state.Collection(...)` for properties that expose a set of keyed entries. A key selector is required — `TKey` must be a struct — so SyncState can track individual entries for efficient change detection:

```csharp
state.Collection(x => x.Orders)
    .WithKey(x => x.Id)
    .GatherFromAsync<AppDbContext>((db, ct) =>
        db.Orders.Select(o => o.ToDto()).ToListAsync(ct));
```

Collection command handlers receive an **`ICollectionPropertyManager<TEntry, TKey>`** as their manager parameter, which extends `IPropertyManager` with `SetEntry`, `RemoveEntry`, and `GetCurrentEntry`. For the full API and how entry-level changes are batched and committed see [Property & State Managers](#property--state-managers).

---

### Commands

Commands are plain C# objects (class, record, or struct). Register handlers on a property with `.On<TCommand>(...)`:

```csharp
// synchronous
.On<OrderCreatedCommand>((cmd, manager) => manager.SetEntry(cmd.Order))

// asynchronous
.On<OrderDeletedCommand>(async (cmd, manager, ct) =>
{
    await manager.ReloadValueAsync(ct); // or manager.RemoveEntry(cmd.Id)
})

// conditional — only handle if the predicate returns true
.On<OrderUpdatedCommand>(
    cmd => cmd.Status == OrderStatus.Shipped,
    (cmd, manager) => manager.SetEntry(cmd.Order))
```

The same command type can be handled by multiple properties across multiple states — all matching handlers fire within the same digest cycle.

Two built-in commands are always registered automatically on every property:

| Command | Effect |
|---|---|
| `ReloadStateCommand<TState>` | Reloads all properties of a specific state |
| `ReloadAllStatesCommand` | Reloads all properties of every state |

#### Command buffering

`ISyncCommandService` supports manually batching multiple commands into a single digest cycle:

```csharp
commandService.StartCommandBuffer();
await commandService.HandleAsync(new OrderCreatedCommand(...));
await commandService.HandleAsync(new OrderUpdatedCommand(...));
await commandService.ExecuteBufferedCommandsAsync(); // one digest cycle for both
// or commandService.ClearCommandBuffer() to discard
```

---

### Events

A property can emit typed events whenever its value changes. Configure them with `.Emit<TEvent>(...)`:

```csharp
// new value only
.Emit<PriceChangedEvent>(newPrice => new PriceChangedEvent(newPrice))

// new + old value
.Emit<PriceChangedEvent>((newPrice, oldPrice) =>
    newPrice != oldPrice ? new PriceChangedEvent(oldPrice, newPrice) : null)
```

Returning `null` suppresses the event. Multiple `.Emit` calls on the same property are all evaluated independently.

Consume events via `ISyncStateService`:

```csharp
// individual events (unbatched)
await foreach (var evt in syncStateService.GetEventStreamAsync<PriceChangedEvent>(ct)) { }

// one batch per digest cycle
await foreach (var batch in syncStateService.GetBatchedEventStreamAsync<PriceChangedEvent>(ct))
{
    foreach (var evt in batch.Events) { }
}

// guaranteed no-miss: get current state AND all subsequent events atomically
var (state, events) = await syncStateService
    .GetCurrentStateAndSubsequentEventsAsync<MyState, MyEvent>(ct);
```

Events can be used to build a custom Transport layer for clients or logging purposes.
Basically anything that benefits from specific event types instead of raw state snapshots. SyncState itself doesn't care about the content of events — it just queues them up during a digest cycle and publishes them to subscribers after the new state is emitted.

> **Note:** `GetEventStreamAsync` / `GetBatchedEventStreamAsync` called on their own are subject to a start-up race condition. Use `GetCurrentStateAndSubsequentEventsAsync` (or `GetCurrentStateAndSubsequentBatchedEventsAsync`) whenever you need to ensure no events are missed after reading the initial state.

---

### Equality comparers

By default SyncState uses `EqualityComparer<TProperty>.Default` to detect whether a property actually changed. Override with `.WithEqualityComparer(...)` when you need custom comparison (e.g. sequence equality for collections, or comparing only certain fields):

```csharp
state.Property(x => x.Config)
    .GatherFrom<IConfigService>(s => s.Current)
    .WithEqualityComparer(new ConfigEqualityComparer());
```

---

## Property & State Managers

Every configured property has a dedicated **property manager** instance, and every registered state has a **state manager** instance. These are created once at startup and live for the lifetime of the application — they are not request-scoped.

Understanding the manager layer explains how SyncState guarantees that state changes are never partially visible to subscribers.

### `IPropertyManager<TProperty>`

One `IPropertyManager<TProperty>` is created per configured property. It is the runtime home of that property's value and the object command handlers directly interact with.

| Member | Description |
|---|---|
| `SetValue(TProperty newValue)` | Stages a new value for the current digest cycle; not visible to subscribers until the cycle commits |
| `GetCurrentValue(bool allowUnpublished = false)` | Returns the last committed value; pass `true` to include values staged in the current cycle |
| `ReloadValueAsync(CancellationToken)` | Re-runs the configured gatherer and stages the result |

#### Pending vs committed values

Each property manager maintains two layers:

- **Committed value** — the last value that was broadcast to subscribers.
- **Pending value** — a value staged by `SetValue` or `ReloadValueAsync` during the active digest cycle, not yet published.

At commit time the equality comparer is applied. If pending equals committed the property is skipped entirely — no state update, no events emitted. If it differs, the pending value is promoted to committed and any configured event emitters fire.

If the digest cycle is aborted (e.g. an exception), `DiscardChanges` is called and all pending values are thrown away — the committed state is never left in a half-applied condition.

### `ICollectionPropertyManager<TEntry, TKey>`

Collection properties use a specialised subtype of `IPropertyManager<IEnumerable<TEntry>>` that adds entry-level mutation:

| Member | Description |
|---|---|
| `SetValue(IEnumerable<TEntry> entries)` | Replace the entire collection |
| `GetCurrentValue(bool allowUnpublished = false)` | Returns the current (or pending) collection |
| `ReloadValueAsync(CancellationToken)` | Re-runs the configured gatherer |
| `SetEntry(TEntry entry)` | Insert or update a single entry; key derived from the configured key selector |
| `RemoveEntry(TKey key)` | Mark an entry for removal |
| `GetCurrentEntry(TKey key, bool includeUnpublished = false)` | Look up the committed (or pending) entry by key |

Internally the committed state is an **`ImmutableDictionary<TKey, TEntry>`**. Pending changes live in two separate buffers — a `Dictionary<TKey, TEntry>` for upserts and a `HashSet<TKey>` for removals — merged into a new immutable dictionary at commit time. Multiple `SetEntry` / `RemoveEntry` calls within one handler therefore collapse into a single atomic state snapshot.

Two deduplication rules are also applied within a cycle: calling `SetEntry` for an entry that was previously `RemoveEntry`-d undoes the removal; calling `SetEntry` twice with equal values (per the configured entry equality comparer) is a no-op.

### `IStateManager<TState>`

`StateManager<TState>` owns all property managers for a single state type. Its responsibilities:

- Creates all `IPropertyManager` instances during initialization and runs every gatherer to populate the initial values.
- Runs the interceptor pipeline around `InitializeAsync` and `HandleCommandAsync`.
- After `CommitChangesAsync`: calls `ApplyToStateObject` on each property manager to assemble a fresh `TState` via `Activator.CreateInstance`, then writes it to all active subscriber channels (bounded, capacity 1, `DropOldest`).
- `DiscardChanges` rolls back all pending values if a digest cycle is aborted.

---

## The Digest Cycle

A digest cycle is the atomic unit of work that takes one or more commands, invokes their handlers, and produces a consistent new state snapshot. Only one cycle runs at a time

When `ISyncCommandService.HandleAsync` is called (or `ExecuteBufferedCommandsAsync` commits a buffer):

1. **Acquires the digest lock** — only one cycle runs at a time, ensuring consistent state.
2. **Dispatches the command** to every state manager; each forwards it to any property manager whose configured handlers match.
3. **Commits changes** — each property manager compares its pending value against the committed value (via the configured `IEqualityComparer<TProperty>`). If different, the value is promoted to committed and any configured event emitters are triggered (see [Events](#events)).
4. **Reconstructs the state object** — the state manager calls `ApplyToStateObject` on every property manager, assembles a fresh `TState` instance, and writes it to every active subscriber channel.
5. **Broadcasts queued events** — the event hub flushes all queued events to subscriber channels.
6. **Releases the lock**.

If any step throws, `DisposeCommandDigestCycle` discards all pending changes and queued events before releasing the lock — the committed state is never partially updated.

---

## SignalR Integration (`SyncState.SignalR`)

### Installation

```shell
dotnet add package SyncState.SignalR
```

`SyncState.SignalR` provides `SyncStateHub` — an abstract base class for SignalR hubs that exposes protected helper methods for streaming state to clients.

### Full-state streaming

Send the entire state object on every change:

```csharp
public class AppStateHub : SyncStateHub
{
    public AppStateHub(ISyncStateService syncStateService) : base(syncStateService) { }

    public IAsyncEnumerable<ApplicationStateDto> StreamState(CancellationToken cancellationToken)
        => GetStateStream<ApplicationStateDto>(cancellationToken);
}
```

### Delta-encoded streaming

Send only the JSON patch diff between consecutive state versions — far more efficient for large state objects:

```csharp
// 1. Enable delta support at registration time
builder.Services.AddSyncState(config =>
{
    config.EnableStateDeltas();   // from SyncState.StateDeltas
    config.AddState<ApplicationStateDto>(state => { ... });
});

// 2. Stream deltas from the hub
public class AppStateHub : SyncStateHub
{
    public AppStateHub(ISyncStateService syncStateService) : base(syncStateService) { }

    public async IAsyncEnumerable<StateDelta> StreamDeltas(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var delta in GetDeltaEncodedStateStream<ApplicationStateDto>(cancellationToken))
            yield return delta;
    }
}
```

`GetDeltaEncodedStateStream` automatically:
1. Sends the full initial state to the caller via `ISyncStateClient.ReceiveDeltaEncodingInitialStateAsync`.
2. Yields a `StateDelta` for every subsequent change.

#### `StateDelta`

```csharp
public record StateDelta
{
    public JsonNode? Patch { get; init; }   // RFC-6902-style JSON patch
    public bool HasChanges { get; }         // false when state was re-emitted but didn't change
}
```

The patch is produced by [`SystemTextJson.JsonDiffPatch`](https://github.com/weichch/system-text-json-jsondiffpatch). By default array objects are matched by an `id` field; configure this via `EnableStateDeltas(jsonDiffOptions: ...)`:

```csharp
config.EnableStateDeltas(jsonOptions: mySerializerOptions);
```

### `ISyncStateClient`

Implement this interface on your JavaScript/TypeScript client:

```csharp
public interface ISyncStateClient
{
    Task ReceiveDeltaEncodingInitialStateAsync(object state, CancellationToken cancellationToken);
}
```

Map the hub in `Program.cs`:

```csharp
app.MapHub<AppStateHub>("/hubs/appstate");
```

---

## Entity Framework Core Integration (`SyncState.EntityFrameworkCore`)

### Installation

```shell
dotnet add package SyncState.EntityFrameworkCore
```

### How it works

The EF Core package hooks into `DbContext.SaveChanges` via an EF Core `ISaveChangesInterceptor`. When changes are saved, it inspects the change tracker, maps affected entities to their corresponding DTO entries, and dispatches SyncState commands — all without you writing any change-detection code.

The integration is built around an **aggregate** pattern: a root entity (that gets mapped to the DTO) owns a set of child entities which are required for mapping. When any entity in the aggregate changes (root or child), the root is re-mapped to a DTO and the corresponding collection entry in the state is updated.

### Setup

Two registrations are required:

```csharp
builder.Services.AddSyncState(config =>
{
    // 1. Enable the EF Core interceptor for your DbContext
    config.EnableEfCoreProvider<AppDbContext>();

    config.AddState<ApplicationStateDto>(state =>
    {
        state.Collection(x => x.Orders)
            .WithKey(x => x.Id)
            // 2. Wire the collection to the EF Core provider
            .GatherFromAsync<AppDbContext>((db, ct) =>
                db.Orders.Select(o => o.ToDto()).ToListAsync(ct))
            .WithEfCoreProvider()
            .FromEntity<Order>(x => x.Id)          // root entity + key
            .WithMapping(order => order.ToDto());   // entity → DTO
    });
});
```

`EnableEfCoreProvider<TDbContext>` registers the `SyncStateDbContextInterceptor` as a scoped service and adds it to EF Core's interceptor pipeline. Call it for every `DbContext` type whose changes should trigger state updates.

#### What `.WithEfCoreProvider()` registers

Calling `.WithEfCoreProvider().FromEntity<TEntity>(...)` automatically wires three command handlers onto the collection property — you never write these yourself:

| Command | Registered handler |
|---|---|
| `AggregateCreatedCommand<TEntry>` | `manager.SetEntry(cmd.Aggregate)` |
| `AggregateUpdatedCommand<TEntry>` | `manager.SetEntry(cmd.Aggregate)` |
| `AggregateDeletedCommand<TEntry, TKey>` | `manager.RemoveEntry(cmd.Key)` |

It also registers two scoped services per aggregate type:
- **`AggregateRootChangeHandler`** — inspects the EF change tracker on `SaveChanges` and records the aggregate state (`Added`, `Updated`, `Deleted`, or `AggregateParticipantChanged` for child-entity changes) into a scoped `AggregateStateStore`.
- **`AggregateCommandDispatcher`** — after `SaveChanges` completes (or the transaction commits), reads the `AggregateStateStore`, maps each changed root entity to a DTO using the configured mapping function, and dispatches the appropriate `AggregateCreatedCommand` / `AggregateUpdatedCommand` / `AggregateDeletedCommand` through `ISyncCommandService`.

These dispatched commands are then handled by the three handlers above, which update the collection via the standard property manager, resulting in a normal digest cycle and state broadcast.

> **Important:** `SyncStateDbContextInterceptor` must be added to your `DbContext` options. The recommended approach is to register it via DI and resolve it in `OnConfiguring` or via `AddDbContext`:
>
> ```csharp
> builder.Services.AddDbContext<AppDbContext>((sp, options) =>
>     options.UseSqlite(connectionString)
>            .AddInterceptors(sp.GetRequiredService<SyncStateDbContextInterceptor>()));
> ```

### Child entities (`WithAdditionalEntity`)

Register child entities that participate in the aggregate so that changes to them also trigger a re-map of their parent root:

```csharp
.WithEfCoreProvider()
.FromEntity<Order>(x => x.Id)
.WithAdditionalEntity<OrderItem>(item => (int?)item.OrderId)
.WithMapping(order => order.ToDto());
```

`WithAdditionalEntity<TAdditionalEntity>` takes a selector that maps the child back to its root key (`TKey?`). When an `OrderItem` is inserted, updated, or deleted, SyncState locates the parent `Order` by that key and re-dispatches it through the mapping pipeline.

For many-to-many or other relationships where a child references multiple roots:

```csharp
.WithAdditionalEntity<Tag>(
    tag => tag.OrderIds,                              // current root keys
    (entry, tracker) => entry.OriginalValues         // original root keys (for moves)
        .GetValue<IEnumerable<int>>("OrderIds"))
```

### Filtering

By default all root entities of the configured type are eligible. Restrict with `.WithFilter(...)`:

```csharp
.WithEfCoreProvider()
.FromEntity<Order>(x => x.Id)
.WithFilter(order => order.IsPublished)
.WithMapping(order => order.ToDto());
```

If a previously included entity no longer passes the filter after an update, a delete command is dispatched for it automatically.

A common use case is **soft deletion**. If your entity has an `IsDeleted` flag, exclude deleted rows from both the initial gatherer query and the filter so hard-deletes and soft-deletes are handled uniformly:

```csharp
state.Collection(x => x.Orders)
    .WithKey(x => x.Id)
    .GatherFromAsync<AppDbContext>((db, ct) =>
        db.Orders
            .Where(o => !o.IsDeleted)           // exclude soft-deleted on initial load
            .Select(o => o.ToDto())
            .ToListAsync(ct))
    .WithEfCoreProvider()
    .FromEntity<Order>(x => x.Id)
    .WithFilter(order => !order.IsDeleted)      // re-evaluated on every SaveChanges
    .WithMapping(order => order.ToDto());
```

When `IsDeleted` is set to `true` and `SaveChanges` is called, the entity fails the filter and an `AggregateDeletedCommand` is dispatched — removing it from the live state without any extra code.  
If the entity was already not present before in the CollectionManager (because it was already IsDeleted before the update), the collection manager just ignores the delete command, so no unnecessary state update is triggered.

### Mapping overloads

| Method | Description |
|---|---|
| `WithMapping(Func<TEntity, TEntry>)` | Synchronous entity → DTO |
| `WithAsyncMapping(Func<TEntity, Task<TEntry>>)` | Async entity → DTO |
| `WithAsyncMapping(Func<TEntity, CancellationToken, Task<TEntry>>)` | Async with cancellation |
| `WithMapping<TService>(Func<TEntity, TService, TEntry>)` | Synchronous with a DI-resolved service |
| `WithAsyncMapping<TService>(Func<TEntity, TService, Task<TEntry>>)` | Async with a DI-resolved service |
| `WithAsyncMapping<TService>(Func<TEntity, TService, CancellationToken, Task<TEntry>>)` | Async with service + cancellation |

### Transaction support

By default SyncState waits for the database transaction to commit before dispatching state commands (`waitForTransactionCompletion: true`). This ensures clients never receive a state update that gets rolled back:

```csharp
config.EnableEfCoreProvider<AppDbContext>(waitForTransactionCompletion: false); // dispatch immediately on SaveChanges
```

When using `waitForTransactionCompletion: true` the interceptor also implements `IDbTransactionInterceptor` and dispatches on `TransactionCommitted`.

> **Known EF Core bug:** If your mapping function triggers a database query (e.g. loads a navigation property) while running inside `TransactionCommitted`, EF Core may throw due to a lazy-loading conflict. This is a [known EF Core issue (#37642)](https://github.com/dotnet/efcore/issues/37642). As a workaround, set `waitForTransactionCompletion: false` to dispatch on `SaveChanges` instead, or ensure your mapping function does not issue additional queries against the same `DbContext`.

---

## Add-on Packages

### `SyncState.ErrorHandling` — Retry & Fallback

```shell
dotnet add package SyncState.ErrorHandling
```

Adds resilience to property initialization and command handling via property-level interceptors.

#### Retry

Automatically retries a failing gatherer or command handler with exponential backoff and optional jitter:

```csharp
// default options (3 retries, 100 ms initial delay, ×2 backoff, jitter on)
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithRetry();

// custom options
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithRetry(options =>
    {
        options.MaxRetries = 5;
        options.InitialDelay = TimeSpan.FromMilliseconds(200);
        options.MaxDelay = TimeSpan.FromSeconds(60);
        options.BackoffMultiplier = 3.0;
        options.UseJitter = true;
        options.ShouldRetry = ex => ex is HttpRequestException; // optional filter
    });
```

`RetryExtension` defaults:

| Property | Default |
|---|---|
| `MaxRetries` | `3` |
| `InitialDelay` | `100 ms` |
| `MaxDelay` | `30 s` |
| `BackoffMultiplier` | `2.0` |
| `UseJitter` | `true` |
| `JitterFactor` | `0.25` |

After all retries are exhausted `RetryExhaustedException` is thrown (wrapping the last exception). Retry applies to both initialization and command handling.

#### Fallback

Provides a substitute value when initialization or command handling throws, preventing the startup failure from propagating:

```csharp
// constant fallback
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithFallback(0m);

// factory fallback
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithFallback(() => GetDefaultPrice());

// service-resolved fallback
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithFallbackFrom<MyState, decimal, ICacheService>(cache => cache.LastKnownPrice);

// exception-aware fallback
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithFallback((sp, ex) => ex is TimeoutException ? 0m : throw ex);
```

Retry and fallback can be combined — but order matters. Because each `.WithXxx()` call adds the interceptor as the new outermost wrapper, `.WithRetry().WithFallback()` produces `Fallback(Retry(target))`: Retry executes first (inner), exhausts its attempts and throws `RetryExhaustedException`, then Fallback catches it (outer). This is the correct order:

```csharp
state.Property(x => x.Price)
    .GatherFromAsync<IPricingService>(...)
    .WithRetry(o => o.MaxRetries = 3)   // inner — retries the gatherer
    .WithFallback(0m);                  // outer — catches RetryExhaustedException
```

> **Interceptor ordering rule:** the last `.WithInterceptor` / `.WithRetry` / `.WithFallback` call registered is the outermost wrapper. Reversing the order above (`.WithFallback().WithRetry()`) would be wrong — Fallback would silently swallow the first error before Retry ever got a chance.

---

### `SyncState.ReloadInterval` — Timer-based Reloads

```shell
dotnet add package SyncState.ReloadInterval
```

Reloads a property's gatherer automatically on a fixed interval using a background worker:

```csharp
state.Property(x => x.ActiveUserCount)
    .GatherFrom<IActiveUserStore>(s => s.GetUsers().Count)
    .ReloadEvery(TimeSpan.FromSeconds(30));
```

Properties configured with the **same `TimeSpan` value** share a single timer tick — the background worker fires once for all of them, dispatching a single `TimedReloadCommand { Interval = ... }` that all matching properties handle. Multiple distinct intervals register independent timers, all managed by a single `ReloadBackgroundWorker` hosted service.

Multiple properties with different intervals:

```csharp
state.Property(x => x.ActiveUserCount)
    .GatherFrom<IActiveUserStore>(...)
    .ReloadEvery(TimeSpan.FromSeconds(30));   // tick A

state.Property(x => x.ExchangeRate)
    .GatherFromAsync<IRatesService>(...)
    .ReloadEvery(TimeSpan.FromMinutes(5));    // tick B
```

---

### `SyncState.OptionsMonitor` — Configuration-driven Properties

```shell
dotnet add package SyncState.OptionsMonitor
```

Binds a property to `IOptionsMonitor<TOption>` so it is automatically updated whenever the underlying configuration source changes (e.g. `appsettings.json` reloaded at runtime):

```csharp
// property type matches the options type directly
state.Property(x => x.FeatureFlags)
    .GatherFromOptionsMonitor<MyState, FeatureFlagsOptions>();

// property type differs — provide a mapping function
state.Property(x => x.MaxPageSize)
    .GatherFromOptionsMonitor<MyState, int, PaginationOptions>(o => o.MaxPageSize);
```

Under the hood this is equivalent to:

```csharp
builder.GatherFrom<IOptionsMonitor<TOption>>(m => mappingFunc(m.CurrentValue));
builder.On<OptionsPropertyChangeCommand<TProperty>>((cmd, pm) => pm.SetValue(cmd.NewValue));
// + registers an IOptionsMonitor.OnChange callback that dispatches OptionsPropertyChangeCommand
```

The `OnChange` callback fires synchronously on the `IOptionsMonitor` thread so SyncState dispatches the command synchronously (blocking until the digest cycle completes) and then returns. The scope used for `ISyncCommandService` is created and disposed within the callback.

---

## Interceptors

Interceptors are middleware hooks that wrap the two core operations on any manager — **initialization** and **command handling**. They let you add cross-cutting behaviour (logging, auth, metrics, resilience) without modifying state or property configuration.

### Two interceptor levels

| Interface | Scope | Registered on |
|---|---|---|
| `IStateInterceptor<TState>` | Wraps the entire state manager | `state.WithInterceptor<T>()` |
| `IPropertyInterceptor<TProperty>` | Wraps a single property manager | `.Property(...).WithInterceptor<T>()` |

Both interfaces inherit the marker `ISyncStateInterceptor` and provide default `next`-passthrough implementations for both hooks, so you only override the methods you need.

### `IStateInterceptor<TState>`

```csharp
public interface IStateInterceptor<TState> : ISyncStateInterceptor where TState : class
{
    Task HandleCommandAsync<TCommand>(
        StateCommandContext<TState, TCommand> context,
        Func<StateCommandContext<TState, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context, cancellationToken);

    Task InitializeAsync(
        StateInitializationContext<TState> context,
        Func<StateInitializationContext<TState>, CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context, cancellationToken);
}
```

Context objects available:

| Context | Members |
|---|---|
| `StateCommandContext<TState, TCommand>` | `Command`, `IStateManager<TState> StateManager`, `StateConfiguration<TState> Configuration` |
| `StateInitializationContext<TState>` | `IStateManager<TState> StateManager`, `StateConfiguration<TState> Configuration` |

### `IPropertyInterceptor<TProperty>`

```csharp
public interface IPropertyInterceptor<TProperty> : ISyncStateInterceptor
{
    Task HandleCommandAsync<TCommand>(
        PropertyCommandContext<TProperty, TCommand> context,
        Func<PropertyCommandContext<TProperty, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context, cancellationToken);

    Task InitializeAsync(
        PropertyInitializationContext<TProperty> context,
        Func<PropertyInitializationContext<TProperty>, CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context, cancellationToken);
}
```

Context objects available:

| Context | Members |
|---|---|
| `PropertyCommandContext<TProperty, TCommand>` | `Command`, `IPropertyManager<TProperty> PropertyManager`, `PropertyConfiguration<TProperty> Configuration` |
| `PropertyInitializationContext<TProperty>` | `IPropertyManager<TProperty> PropertyManager`, `PropertyConfiguration<TProperty> Configuration` |

### Registering interceptors

Interceptors are resolved from the DI container per digest cycle scope (transient recommended). Registering via the builder handles DI registration automatically:

```csharp
// state-level
config.AddState<AppStateDto>(state =>
{
    state.WithInterceptor<MyStateLoggingInterceptor>();
    state.Property(x => x.Price)
        .GatherFromAsync<IPricingService>(...)
        .WithInterceptor<MyPropertyMetricsInterceptor>();
});
```

### Pipeline & ordering

Interceptors are composed into a middleware pipeline using `next` delegates — the same pattern as ASP.NET Core middleware. Each interceptor receives a `next` function that calls the next layer inward.

**Last registered = outermost wrapper.** Given `.WithInterceptor<A>().WithInterceptor<B>()`, execution order is `B → A → target`. B's `next` calls A, A's `next` calls the actual initialization or command handler.

```
registration:  [A, B]
execution:      B( A( target ) )
```

This is relevant when combining interceptors whose behaviour depends on ordering — for example the retry + fallback pattern from `SyncState.ErrorHandling` (see [Retry & Fallback](#syncstateerrorhandling--retry--fallback)).

### Example: logging interceptor

```csharp
public class OrdersCommandLoggingInterceptor
    : IStateInterceptor<ApplicationStateDto>
{
    private readonly ILogger<OrdersCommandLoggingInterceptor> _logger;

    public OrdersCommandLoggingInterceptor(ILogger<OrdersCommandLoggingInterceptor> logger)
        => _logger = logger;

    public async Task HandleCommandAsync<TCommand>(
        StateCommandContext<ApplicationStateDto, TCommand> context,
        Func<StateCommandContext<ApplicationStateDto, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling {Command}", typeof(TCommand).Name);
        await next(context, cancellationToken);
        _logger.LogDebug("Handled {Command}", typeof(TCommand).Name);
    }
}
```

```csharp
state.WithInterceptor<OrdersCommandLoggingInterceptor>();
```

