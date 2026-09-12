using System.Reflection;
using SyncState.Configuration.Interfaces;

namespace SyncState.Configuration.Builder;

/// <summary>
/// A property configuration class registered for automatic assignment to state properties without explicit configuration.
/// </summary>
internal abstract class PropertyConfigurationRegistration
{
    /// <summary>
    /// Precedence of a configuration whose property type equals the type of the property.
    /// </summary>
    public const int ExactTypeMatch = 3;

    /// <summary>
    /// Precedence of a collection configuration whose <see cref="IEnumerable{T}"/> the property type implements.
    /// </summary>
    public const int ImplementedEnumerableMatch = 2;

    /// <summary>
    /// Precedence of a collection configuration whose <see cref="IEnumerable{T}"/> the property type is assignable to.
    /// </summary>
    public const int AssignableEnumerableMatch = 1;

    /// <summary>
    /// The configuration does not match the property.
    /// </summary>
    public const int NoMatch = 0;

    public abstract Type ConfigurationType { get; }

    /// <summary>
    /// Returns how specifically this configuration matches <paramref name="property"/>; higher values take precedence.
    /// </summary>
    public abstract int GetMatchPrecedence(PropertyInfo property);

    /// <summary>
    /// Creates the property builder for <paramref name="property"/> with this configuration applied.
    /// </summary>
    public abstract PropertyConfigurationBuilder CreateBuilder<TState>(StateConfigurationBuilder<TState> stateBuilder,
        PropertyInfo property) where TState : class;
}

internal sealed class PropertyConfigurationRegistration<TProperty, TConfiguration> : PropertyConfigurationRegistration
    where TConfiguration : IPropertyConfiguration<TProperty>, new()
{
    public override Type ConfigurationType => typeof(TConfiguration);

    public override int GetMatchPrecedence(PropertyInfo property)
    {
        return property.PropertyType == typeof(TProperty) ? ExactTypeMatch : NoMatch;
    }

    public override PropertyConfigurationBuilder CreateBuilder<TState>(StateConfigurationBuilder<TState> stateBuilder,
        PropertyInfo property)
    {
        var builder = new PropertyConfigurationBuilder<TState, TProperty>(stateBuilder, property);
        new TConfiguration().Configure(builder);
        return builder;
    }
}

internal sealed class CollectionPropertyConfigurationRegistration<TEntry, TKey, TConfiguration> : PropertyConfigurationRegistration
    where TConfiguration : ICollectionPropertyConfiguration<TEntry, TKey>, new()
    where TKey : struct
{
    public override Type ConfigurationType => typeof(TConfiguration);

    public override int GetMatchPrecedence(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (propertyType == typeof(IEnumerable<TEntry>) || propertyType.GetInterfaces().Contains(typeof(IEnumerable<TEntry>)))
        {
            return ImplementedEnumerableMatch;
        }

        return typeof(IEnumerable<TEntry>).IsAssignableFrom(propertyType) ? AssignableEnumerableMatch : NoMatch;
    }

    public override PropertyConfigurationBuilder CreateBuilder<TState>(StateConfigurationBuilder<TState> stateBuilder,
        PropertyInfo property)
    {
        var configuration = new TConfiguration();
        var builder = new CollectionPropertyBuilder<TState, TEntry, TKey>(stateBuilder, property, configuration.KeySelector);
        configuration.Configure(builder);
        return builder;
    }
}
