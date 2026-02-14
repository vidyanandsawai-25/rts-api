namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a department in the system
/// </summary>
public class DepartmentMasterEntity : CommonBaseEntity
{
    /// <summary>
    /// Unique identifier for the department
    /// </summary>
    public int DepartmentMasterId { get; set; }

    /// <summary>
    /// Unique code for the department
    /// </summary>
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Name of the department
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Name of the department in local language
    /// </summary>
    public string? DepartmentNameLocal { get; set; }

    /// <summary>
    /// Icon for the department
    /// </summary>
    public string? DepartmentIcon { get; set; }

    /// <summary>
    /// Description of the department
    /// </summary>
    public string? DepartmentDescription { get; set; }

    public DepartmentMasterEntity Department { get; set; }
}
