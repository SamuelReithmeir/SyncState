using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SyncState.EntityFrameworkCore.Interception;

public record EntityChangeEntry
{
    public required EntityEntry Entry { get; init; }
    public required EntityState State { get; init; }   
}