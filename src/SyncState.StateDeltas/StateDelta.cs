using System.Text.Json.Nodes;

namespace SyncState.StateDeltas;

/// <summary>
/// Represents a delta (difference) between two state versions using JSON patch format.
/// </summary>
public class StateDelta
{
    /// <summary>
    /// Gets the JSON patch representing the delta between state versions.
    /// </summary>
    public required JsonNode? Delta { get; init; }
}