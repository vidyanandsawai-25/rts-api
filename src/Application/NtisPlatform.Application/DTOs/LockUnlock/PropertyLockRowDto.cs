namespace NtisPlatform.Application.DTOs.LockUnlock;

public class PropertyLockRowDto
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string PartitionNo { get; set; } = string.Empty;

    public bool IsLocked { get; set; }
    public List<LockableScreenDto> LockedScreens { get; set; } = new();
}
