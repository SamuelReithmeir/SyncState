using SyncState.Interfaces.Managers;

namespace SyncState.Models.InterceptorContexts;

public record PropertyCommandContext<TProperty,TCommand>(TCommand Command, IPropertyManager<TProperty> PropertyManager);