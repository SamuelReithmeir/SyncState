namespace SyncState.Sample.DTOs;

public class ApplicationStateDto
{
    public required List<OrderDto> Orders { get; set; }
    public int CurrentActiveUserCount { get; set; }
}