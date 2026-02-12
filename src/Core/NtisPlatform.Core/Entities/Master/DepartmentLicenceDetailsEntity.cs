using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Department Licence Details entity for managing department-wise licence information
/// Maps to collective.DepartmentLicenceDetails table
/// </summary>
public class DepartmentLicenceDetailsEntity : CommonBaseEntity
{
    /// <summary>
    /// Primary key - Licence Details ID
    /// </summary>
    public int LicenceDetailsId { get; set; }

    /// <summary>
    /// Foreign key to Department Master
    /// </summary>
    public int? DepartmentMasterId { get; set; }

    /// <summary>
    /// Licence start date
    /// </summary>
    public DateTime? LicenceStartDate { get; set; }

    /// <summary>
    /// Licence end date
    /// </summary>
    public DateTime? LicenceEndDate { get; set; }

    /// <summary>
    /// Licence duration (e.g., "1 Year", "6 Months")
    /// </summary>
    public string? LicenceDuration { get; set; } 

    /// <summary>
    /// Navigation property to Department Master
    /// </summary>
    public DepartmentMasterEntity? Department { get; set; }
}
