using System.Linq.Expressions;
using SyncState.Configuration.Interfaces;

namespace SyncState.EntityFrameworkCore.Configuration;

public class PartialEfCoreCollectionBuilder<TState, TEntry, TKey> : IPartialEfCoreCollectionBuilder<TState, TEntry, TKey>
    where TState : class where TKey : struct
{
    private readonly ICollectionPropertyBuilder<TState, TEntry, TKey> _builder;

    public PartialEfCoreCollectionBuilder(ICollectionPropertyBuilder<TState, TEntry, TKey> builder)
    {
        _builder = builder;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> FromEntity<TEntity>(
        Expression<Func<TEntity, TKey>> keySelector) where TEntity : class
    {
        return new EfCoreCollectionBuilder<TState, TEntry, TEntity, TKey>(_builder, keySelector);
    }
}