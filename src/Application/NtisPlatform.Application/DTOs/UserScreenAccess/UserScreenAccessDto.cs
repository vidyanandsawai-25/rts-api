namespace NtisPlatform.Application.DTOs.UserScreenAccess;

/// <summary>
/// DTO representing user screen access with department, module, and screen information
/// </summary>
public class UserScreenAccessDto
{
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public int UserId { get; set; }
    public int UserRoleId { get; set; }
    public string? ScreenCode { get; set; }
    public string? ScreenName { get; set; }
    public string? ScreenNameLocal { get; set; }
    public string? ScreenIcon { get; set; }
    public string? RoutePath { get; set; }
    public bool? IsMenu { get; set; }
    
    // Permission flags from RoleWiseScreenAccessMaster
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool HaveFullAccess { get; set; }
    public bool HaveNoAccess { get; set; }
    public string? ScreenGroupName { get; set; }
}
