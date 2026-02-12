using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;

/// <summary>
/// Query parameters for filtering and searching Department Licence Details
/// </summary>
public class DepartmentLicenceDetailsQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by department
    /// </summary>
    [Filterable]
    public int? DepartmentMasterId { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by licence duration
    /// </summary>
    [Filterable]
    public string? LicenceDuration { get; set; }
}
