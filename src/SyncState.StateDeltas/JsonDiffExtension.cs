using System.Text.Json.JsonDiffPatch;

namespace SyncState.StateDeltas;

public class JsonDiffExtension
{
    public JsonDiffOptions JsonDiffOptions { get; set; } = new();
}