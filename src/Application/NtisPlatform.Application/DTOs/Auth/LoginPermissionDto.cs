namespace NtisPlatform.Application.DTOs.Auth;

public class LoginPermissionDto
{
    public int DepartmentId { get; set; }
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
}
