namespace NtisPlatform.Application.DTOs.LockUnlock;

public class LockableScreenDto
{
    public int Id { get; set; }
    public string ScreenCode { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty;
    public string? ScreenNameLocal { get; set; }
    public int? DisplayOrder { get; set; }
    public int? ModuleId { get; set; }
    public string? ModuleCode { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleNameLocal { get; set; }
    public string? ModuleLabel { get; set; }
}
