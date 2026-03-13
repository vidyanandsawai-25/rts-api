namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a module in the system
/// </summary>
public class ModuleMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique identifier for the module
    /// </summary>
    public int ModuleId { get; set; } 

    /// <summary>
    /// Foreign key to the department this module belongs to 
    /// </summary>
    public int DepartmentId { get; set; } 

    /// <summary>
    /// Unique code for the module
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// Name of the module 
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Name of the module in local language
    /// </summary>
    public string? ModuleNameLocal { get; set; }

    /// <summary>
    /// Icon for the module
    /// </summary>
    public string? ModuleIcon { get; set; }

    /// <summary>
    /// Label for the module
    /// </summary>
    public string? ModuleLabel { get; set; }

    /// <summary>
    /// Description of the module
    /// </summary>
    public string? ModuleDescription { get; set; }

 

    /// <summary>
    /// Navigation property to the department
    /// </summary>
    public DepartmentMasterEntity? Department { get; set; }
}
