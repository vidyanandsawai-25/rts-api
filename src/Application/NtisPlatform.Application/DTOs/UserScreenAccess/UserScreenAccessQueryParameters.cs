using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.UserScreenAccess;

/// <summary>
/// Query parameters for filtering user screen access data
/// </summary>
public class UserScreenAccessQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by specific user ID
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Filter by user role ID
    /// </summary>
    public int? UserRoleId { get; set; }
    
    /// <summary>
    /// Filter by department ID
    /// </summary>
    public int? DepartmentId { get; set; }
    
    /// <summary>
    /// Filter by module ID
    /// </summary>
    public int? ModuleId { get; set; }
}
