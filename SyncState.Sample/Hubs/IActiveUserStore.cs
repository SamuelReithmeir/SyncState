namespace SyncState.Sample.Hubs;

public interface IActiveUserStore
{
    Task AddUserAsync(string connectionId);
    Task RemoveUserAsync(string connectionId);
    IReadOnlyCollection<string> GetActiveUsers();
    bool IsUserActive(string connectionId);
}

