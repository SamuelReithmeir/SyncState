using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;

namespace SyncState.Models.InterceptorContexts;

public record PropertyCommandContext<TProperty, TCommand>(
    TCommand Command,
    IPropertyManager<TProperty> PropertyManager,
    PropertyConfiguration<TProperty> Configuration);
