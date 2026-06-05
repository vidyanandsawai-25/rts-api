namespace NtisPlatform.Application.DTOs.LockUnlock;

public class LockableScreenDto
{
    public int Id { get; set; }
    public string ScreenCode { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty;
    public string? ScreenNameLocal { get; set; }
    public int? DisplayOrder { get; set; }
}
