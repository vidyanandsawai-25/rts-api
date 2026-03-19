using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ConfigValueMaster;

/// <summary>
/// Query parameters for filtering and sorting ConfigValueMaster
/// </summary>
public class ConfigValueMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by configuration key ID
    /// </summary>
    [Filterable]
    [Sortable]
    public int? ConfigKeyId { get; set; }

    /// <summary>
    /// Filter by department ID
    /// </summary>
    [Filterable]
    [Sortable]
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Filter by module ID
    /// </summary>
    [Filterable]
    [Sortable]
    public int? ModuleId { get; set; }

    /// <summary>
    /// Filter by value
    /// </summary>
    [Filterable]
    [Sortable]
    [Searchable]
    public string? Value { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
