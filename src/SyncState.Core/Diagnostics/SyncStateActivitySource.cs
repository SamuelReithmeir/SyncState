using System.Diagnostics;
using System.Reflection;

namespace SyncState.Diagnostics;

/// <summary>
/// Provides the shared <see cref="System.Diagnostics.ActivitySource"/> used by SyncState components to emit
/// OpenTelemetry compatible tracing information. Consumers can enable SyncState tracing by adding a listener
/// (e.g. via <c>AddSource("SyncState")</c> when configuring OpenTelemetry tracing) for the <see cref="Name"/>.
/// </summary>
public static class SyncStateActivitySource
{
    /// <summary>
    /// The name used to register the <see cref="ActivitySource"/> with OpenTelemetry (e.g. via <c>TracerProviderBuilder.AddSource</c>).
    /// </summary>
    public const string Name = "SyncState";

    private static readonly string? Version = typeof(SyncStateActivitySource).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    /// <summary>
    /// The shared <see cref="ActivitySource"/> instance used to start activities across SyncState components.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name, Version);
}

