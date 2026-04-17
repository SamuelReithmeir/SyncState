using System.Text.Json.Nodes;

namespace SyncState.StateDeltas;

/// <summary>
/// Represents a delta (difference) between two state versions using JSON patch format.
/// </summary>
public record StateDelta
{
    /// <summary>
    /// Gets the JSON patch representing the difference between two state versions.
    /// </summary>
    public required JsonNode? Patch { get; init; }

    /// <summary>
    /// Returns true when the state actually changed (i.e. the patch is non-null).
    /// </summary>
    public bool HasChanges => Patch is not null;
}