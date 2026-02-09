namespace SyncState.EntityFrameworkCore.Aggregates;

public interface IAggregateStateStore
{
    public void SetAggregateState<TAggregate, TAggregateRoot, TKey>(TAggregateRoot rootEntity, TKey key,
        AggregateState state) where TKey : struct;

    public IEnumerable<(TAggregateRoot, TKey, AggregateState)> GetAggregateRoots<TAggregate, TAggregateRoot, TKey>()
        where TKey : struct;
    
    public void ClearAggregateStates<TAggregate>();
}

public class AggregateStateStore : IAggregateStateStore
{
    private readonly Dictionary<Type, object> _aggregateStateDictionaries = new();

    private Dictionary<TKey, (TAggregateRoot, TKey, AggregateState)> GetAggregateDictionary<TAggregate, TAggregateRoot,
        TKey>() where TKey : struct
    {
        if (!_aggregateStateDictionaries.TryGetValue(typeof(TAggregate), out var dictObj))
        {
            dictObj = new Dictionary<TKey, (TAggregateRoot, TKey, AggregateState)>();
            _aggregateStateDictionaries[typeof(TAggregate)] = dictObj;
        }

        return (Dictionary<TKey, (TAggregateRoot, TKey, AggregateState)>)dictObj;
    }

    public void SetAggregateState<TAggregate, TAggregateRoot, TKey>(TAggregateRoot rootEntity, TKey key,
        AggregateState state) where TKey : struct
    {
        var dict = GetAggregateDictionary<TAggregate, TAggregateRoot, TKey>();
        dict[key] = (rootEntity, key, state);
    }

    public IEnumerable<(TAggregateRoot, TKey, AggregateState)> GetAggregateRoots<TAggregate, TAggregateRoot, TKey>()
        where TKey : struct
    {
        return GetAggregateDictionary<TAggregate, TAggregateRoot, TKey>().Values;
    }

    public void ClearAggregateStates<TAggregate>()
    {
        _aggregateStateDictionaries.Remove(typeof(TAggregate));
    }
}