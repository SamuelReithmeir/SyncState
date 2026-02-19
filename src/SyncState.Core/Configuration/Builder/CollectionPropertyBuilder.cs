using System.Linq.Expressions;
using SyncState.Configuration.Interfaces;
using SyncState.Enums;
using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;
using SyncState.Models.Configuration.EventEmitters;
using SyncState.Services.Managers;
using SyncState.Utils;

namespace SyncState.Configuration.Builder;

internal class CollectionPropertyBuilder<TState, TEntry, TKey> :
    PropertyConfigurationBuilder<TState, IEnumerable<TEntry>>,
    ICollectionPropertyBuilder<TState, TEntry, TKey> where TState : class where TKey : struct
{
    private readonly Expression<Func<TEntry, TKey>> _keySelector;
    private readonly List<CollectionOnAddEventEmitterConfiguration<TEntry, TKey>> _onAddEventEmitterConfigurations = [];

    private readonly List<CollectionOnRemoveEventEmitterConfiguration<TEntry, TKey>>
        _onRemoveEventEmitterConfigurations = [];

    private readonly List<CollectionOnUpdateEventEmitterConfiguration<TEntry, TKey>>
        _onUpdateEventEmitterConfigurations = [];

    private IEqualityComparer<TEntry>? _entryEqualityComparer;

    public CollectionPropertyBuilder(StateConfigurationBuilder<TState> parentBuilder,
        Expression<Func<TState, IEnumerable<TEntry>>> collectionExpression,
        Expression<Func<TEntry, TKey>> keySelector) : base(
        parentBuilder, collectionExpression.GetPropertyInfo())
    {
        _keySelector = keySelector;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> On<TCommand>(
        Action<TCommand, ICollectionPropertyManager<TEntry, TKey>> handler) where TCommand : notnull
    {
        //call base with cast manager
        base.On<TCommand>((command, manager) => handler(command, (ICollectionPropertyManager<TEntry, TKey>)manager));
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> On<TCommand>(Func<TCommand, bool> commandFilter,
        Action<TCommand, ICollectionPropertyManager<TEntry, TKey>> handler) where TCommand : notnull
    {
        //call base with cast manager
        base.On(commandFilter,
            (command, manager) => handler(command, (ICollectionPropertyManager<TEntry, TKey>)manager));
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnAdd<TEvent>(Func<TEntry, TEvent?> eventFactory)
        where TEvent : notnull
    {
        _onAddEventEmitterConfigurations.Add(new CollectionOnAddEventEmitterConfiguration<TEntry, TKey>
        {
            EmitEvent = (property, eventService) =>
            {
                if (eventFactory(property) is { } evt)
                {
                    eventService.QueueEvent(evt);
                }
            }
        });
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnUpdate<TEvent>(
        Func<TEntry, TEntry, TEvent?> eventFactory) where TEvent : notnull
    {
        _onUpdateEventEmitterConfigurations.Add(new CollectionOnUpdateEventEmitterConfiguration<TEntry, TKey>
        {
            EmitEvent = (oldEntry, newEntry, eventService) =>
            {
                if (eventFactory(oldEntry, newEntry) is { } evt)
                {
                    eventService.QueueEvent(evt);
                }
            }
        });
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnUpdate<TEvent>(Func<TEntry, TEvent?> eventFactory)
        where TEvent : notnull
    {
        return EmitOnUpdate<TEvent>((_, newEntry) => eventFactory(newEntry));
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnRemove<TEvent>(Func<TEntry, TEvent?> eventFactory)
        where TEvent : notnull
    {
        _onRemoveEventEmitterConfigurations.Add(new CollectionOnRemoveEventEmitterConfiguration<TEntry, TKey>
        {
            EmitEvent = (property, _, eventService) =>
            {
                if (eventFactory(property) is { } evt)
                {
                    eventService.QueueEvent(evt);
                }
            }
        });
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnRemove<TEvent>(
        Func<TEntry, TKey, TEvent?> eventFactory) where TEvent : notnull
    {
        _onRemoveEventEmitterConfigurations.Add(new CollectionOnRemoveEventEmitterConfiguration<TEntry, TKey>
        {
            EmitEvent = (property, key, eventService) =>
            {
                if (eventFactory(property, key) is { } evt)
                {
                    eventService.QueueEvent(evt);
                }
            }
        });
        return this;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> WithEntryEqualityComparer(
        IEqualityComparer<TEntry> equalityComparer)
    {
        _entryEqualityComparer = equalityComparer;
        return this;
    }

    public override PropertyConfiguration Build()
    {
        if (Gatherer == null)
        {
            throw new InvalidOperationException(
                $"Gatherer function must be specified for property '{PropertyInfo.Name}'.");
        }

        if (PropertyManagerType == null)
        {
            PropertyManagerType = typeof(CollectionPropertyManager<TEntry, TKey>);
        }

        var scopeBehavior = _ScopeBehavior ?? PropertyGatheringServiceScopeBehavior.ShareScope;
        
        
        var equalityComparer = EqualityComparer ?? EqualityComparer<IEnumerable<TEntry>>.Default;
        var entryEqualityComparer = _entryEqualityComparer ?? EqualityComparer<TEntry>.Default;

        var configuration = new CollectionPropertyConfiguration<TEntry, TKey>
        {
            PropertyInfo = PropertyInfo,
            Gatherer = Gatherer,
            ScopeBehavior = scopeBehavior,
            PropertyManagerType = PropertyManagerType,
            CommandHandlerConfigurations = CommandHandlers,
            EqualityComparer = equalityComparer,
            EntryEqualityComparer = entryEqualityComparer,
            EventEmitters = EventEmitters,
            OnAddEventEmitterConfigurations = _onAddEventEmitterConfigurations,
            OnRemoveEventEmitterConfigurations = _onRemoveEventEmitterConfigurations,
            OnUpdateEventEmitterConfigurations = _onUpdateEventEmitterConfigurations,
            KeySelector = _keySelector,
            InterceptorTypes = InterceptorTypes,
            Extensions = Extensions
        };

        foreach (var buildAction in BuildActions)
        {
            buildAction();
        }

        foreach (var processor in ConfigurationPostProcessors)
        {
            processor(configuration);
        }

        return configuration;
    }
}

/// <summary>
/// intermediate builder for collection properties to allow for smooth generic type inference
/// </summary>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TEntry"></typeparam>
internal class PartialCollectionPropertyBuilder<TState, TEntry> : IPartialCollectionPropertyBuilder<TState, TEntry>
    where TState : class
{
    private readonly IStateConfigurationBuilder<TState> _parentBuilder;
    private readonly Expression<Func<TState, IEnumerable<TEntry>>> _propertyExpression;

    public PartialCollectionPropertyBuilder(StateConfigurationBuilder<TState> parentBuilder,
        Expression<Func<TState, IEnumerable<TEntry>>> propertyExpression)
    {
        _parentBuilder = parentBuilder;
        _propertyExpression = propertyExpression;
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> WithKey<TKey>(Expression<Func<TEntry, TKey>> keyExpression)
        where TKey : struct
    {
        return _parentBuilder.Collection(_propertyExpression, keyExpression);
    }
}