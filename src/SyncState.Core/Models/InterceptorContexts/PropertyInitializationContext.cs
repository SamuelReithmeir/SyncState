using SyncState.Interfaces.Managers;

namespace SyncState.Models.InterceptorContexts;

public record PropertyInitializationContext<TProperty>(IPropertyManager<TProperty> PropertyManager);