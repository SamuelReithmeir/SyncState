using System.Text.Json;

namespace SyncState.Models.Configuration.Extensions;

public class JsonSerializationExtension
{
    public required JsonSerializerOptions JsonSerializerOptions { get; set; }
}