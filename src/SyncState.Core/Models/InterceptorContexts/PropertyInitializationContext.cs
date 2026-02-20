using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;

namespace SyncState.Models.InterceptorContexts;

public record PropertyInitializationContext<TProperty>(
    IPropertyManager<TProperty> PropertyManager,
    PropertyConfiguration<TProperty> Configuration);
