namespace SyncState.Sample.Hubs;

public class ActiveUserStore : IActiveUserStore
{
    private readonly HashSet<string> _activeUsers = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public Task AddUserAsync(string connectionId)
    {
        _lock.EnterWriteLock();
        try
        {
            _activeUsers.Add(connectionId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(string connectionId)
    {
        _lock.EnterWriteLock();
        try
        {
            _activeUsers.Remove(connectionId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<string> GetActiveUsers()
    {
        if (Random.Shared.NextDouble() < 0.5) // Simulate that this method fails 50% of the time to demonstrate failure handling
        {
            throw new InvalidOperationException("Simulated failure in GetActiveUsers");
        }

        _lock.EnterReadLock();
        try
        {
            return _activeUsers.ToList().AsReadOnly();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsUserActive(string connectionId)
    {
        _lock.EnterReadLock();
        try
        {
            return _activeUsers.Contains(connectionId);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}