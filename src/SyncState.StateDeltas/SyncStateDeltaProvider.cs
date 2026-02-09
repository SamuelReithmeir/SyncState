using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using SyncState.Interfaces;
using SyncState.Models.Configuration;
using SyncState.Models.Configuration.Extensions;

namespace SyncState.StateDeltas;

public class SyncStateDeltaProvider : ISyncStateDeltaProvider
{
    private readonly ISyncStateService _syncStateService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly JsonDiffOptions _jsonDiffOptions;

    public SyncStateDeltaProvider(ISyncStateService syncStateService, SyncStateConfiguration configuration)
    {
        _syncStateService = syncStateService;
        if (configuration.GetExtension<JsonSerializationExtension>() is not
            { JsonSerializerOptions: var serializerOptions })
        {
            serializerOptions = new JsonSerializerOptions();
        }

        _jsonSerializerOptions = serializerOptions;

        if (configuration.GetExtension<JsonDiffExtension>() is not { JsonDiffOptions: var jsonDiffOptions })
        {
            jsonDiffOptions = new JsonDiffOptions
            {
                ArrayObjectItemKeyFinder = (element,i) => element?["id"]?.ToString(),
                SuppressDetectArrayMove = true,
                
            };
        }

        _jsonDiffOptions = jsonDiffOptions;
    }

    public async Task<(TState, IAsyncEnumerable<StateDelta>)> GetStateWithDeltas<TState>(
        CancellationToken cancellationToken = default) where TState : class
    {
        var stateEnumerable = _syncStateService.GetStateEnumerable<TState>(cancellationToken);
        var enumerator = stateEnumerable.GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        var state = enumerator.Current;
        return (state, GetStateDeltas(state, enumerator));
    }

    private async IAsyncEnumerable<StateDelta> GetStateDeltas<TState>(TState initial,
        IAsyncEnumerator<TState> stateEnumerator)
    {
        var currentJson = JsonSerializer.SerializeToNode(initial, _jsonSerializerOptions);
        while (await stateEnumerator.MoveNextAsync())
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var nextJson = JsonSerializer.SerializeToNode(stateEnumerator.Current, _jsonSerializerOptions);
            var delta = currentJson.Diff(nextJson, _jsonDiffOptions);
            stopwatch.Stop();
            Console.WriteLine(
                $"[SyncStateDeltaProvider] Computed delta for {typeof(TState).Name} in {stopwatch.ElapsedMilliseconds} ms");
            currentJson = nextJson;
            yield return new StateDelta
            {
                Delta = delta
            };
        }
    }
}