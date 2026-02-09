using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SyncState.Utils;

public static class TypeExtensions
{
    public static PropertyInfo GetPropertyInfo<T, TProperty>(
        this Expression<Func<T, TProperty>> propertyLambda)
    {
        var type = typeof(T);

        if (propertyLambda.Body is not MemberExpression member)
        {
            throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");
        }

        if (member.Member is not PropertyInfo propInfo)
        {
            throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");
        }

        if (type != propInfo.DeclaringType && !type.IsSubclassOf(propInfo.DeclaringType!))
        {
            throw new ArgumentException(
                $"Expression '{propertyLambda}' refers to a property that is not from type {type}.");
        }

        return propInfo;
    }

    /// <summary>
    /// Returns all base types of the given type, including the given type itself.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetBaseTypes(this Type type)
    {
        var currentType = type;
        while (currentType != null)
        {
            yield return currentType;
            currentType = currentType.BaseType;
        }
    }
    
    /// <summary>
    /// adds a scoped service of type TService with implementation TImplementation to the service collection if it does not already exist with this exact implementation
    /// allows adding multiple implementations of the same service interface without overwriting existing ones, which is not possible with the built-in TryAddScoped method
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    public static void TryAddScopedImplementation<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(s => s.ServiceType == typeof(TService)&& s.ImplementationType == typeof(TImplementation)))
        {
            services.AddScoped<TService, TImplementation>();
        }
    }
}