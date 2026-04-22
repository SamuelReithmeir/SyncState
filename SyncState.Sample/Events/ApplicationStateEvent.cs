using System.Text.Json.Serialization;

namespace SyncState.Sample.Events;

[JsonDerivedType(typeof(ActiveUserCountChangedEvent), typeDiscriminator: "activeUserCountChanged")]
public abstract record ApplicationStateEvent();