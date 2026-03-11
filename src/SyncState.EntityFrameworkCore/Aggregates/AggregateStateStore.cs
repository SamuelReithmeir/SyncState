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
        AggregateState? currentState = dict.TryGetValue(key, out var existing) ? existing.Item3 : null;
        switch (currentState, state)
        {
            case (null, _):
                dict[key] = (rootEntity, key, state);
                break;
            case (_, AggregateState.AggregateParticipantChanged):
                dict[key] = dict[key];//every other state is more important than AggregateParticipantChanged, so we just keep the existing state
                break;
            case (AggregateState.Added, AggregateState.Updated):
                dict[key] = (rootEntity, key, AggregateState.Added);//if it's added, we keep it as added even if it's updated later, because it's still a new aggregate
                break;
            case (AggregateState.Added, AggregateState.Deleted):
                dict.Remove(key);//if it's added and then deleted before dispatching, we can just remove it from the dictionary, because it doesn't need to be synchronized
                break;
            default:
                dict[key] = (rootEntity, key, state);
                break;
        }
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