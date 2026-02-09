using SyncState.Interfaces.Managers;

namespace SyncState.Models.Configuration;

public abstract class CommandHandlerConfiguration
{
    /// <summary>
    /// Quick filter over the type used for indexing command listeners
    /// <remarks> true if any command of the given type should be considered; false if no command of that type will ever match </remarks>
    /// </summary>
    public Func<Type, bool> CommandTypeFilter { get; }

    /// <summary>
    /// optional filter over the actual command instance
    /// <example> e.g. only listen to commands with a specific property value </example>
    /// </summary>
    public Func<object, bool>? CommandFilter { get; }

    public Func<object, IPropertyManager, CancellationToken, Task> OnCommandAsync { get; }

    protected CommandHandlerConfiguration(Func<Type, bool> commandTypeFilter, Func<object, bool>? commandFilter,
        Func<object, IPropertyManager, CancellationToken, Task> onCommandAsync)
    {
        CommandTypeFilter = commandTypeFilter;
        CommandFilter = commandFilter;
        OnCommandAsync = onCommandAsync;
    }
}

public class CommandHandlerConfiguration<TCommand, TProperty> : CommandHandlerConfiguration
    where TCommand : notnull
{
    public CommandHandlerConfiguration(Func<TCommand, bool>? commandFilter,
        Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> onCommandAsync
    ) : base(
        t => t.IsAssignableTo(typeof(TCommand)),
        commandFilter == null ? null : e => e is TCommand typed && commandFilter(typed),
        async (e, pm, ct) => { await onCommandAsync((TCommand)e, (IPropertyManager<TProperty>)pm, ct); })
    {
        CommandFilter = commandFilter;
        OnCommandAsync = onCommandAsync;
    }

    public new Func<TCommand, bool>? CommandFilter { get; }
    public new Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> OnCommandAsync { get; }
}