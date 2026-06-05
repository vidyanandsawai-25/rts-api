namespace NtisPlatform.Application.DTOs.LockUnlock;

public class BulkLockRequestDto
{
    public List<int> PropertyIds { get; set; } = new();
    public List<int> ScreenIds { get; set; } = new();
    public string Action { get; set; } = "lock";
}
