using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ULBMaster;

/// <summary>
/// Query parameters for filtering and searching ULB Masters
/// </summary>
public class ULBMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by ULB code
    /// </summary>
    [Filterable]
    [Sortable]
    public string? UlbCode { get; set; }

    /// <summary>
    /// Search in ULB name (English)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? UlbName { get; set; }

    /// <summary>
    /// Search in ULB name (Local)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? UlbNameLocal { get; set; }

    /// <summary>
    /// Filter by ULB type
    /// </summary>
    [Filterable]
    [Sortable]
    public int? UlbTypeId { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by email
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? EmailId { get; set; }

    /// <summary>
    /// Filter by phone number
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? MobileNo { get; set; }

    /// <summary>
    /// Filter by contact person name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Filter by state
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? State { get; set; }

    /// <summary>
    /// Filter by district
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? District { get; set; }

    /// <summary>
    /// Filter by PIN code
    /// </summary>
    [Filterable]
    public string? PinCode { get; set; }

    /// <summary>
    /// Filter by partner name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PartnerName { get; set; }

    /// <summary>
    /// Filter by PM name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PMName { get; set; }

    /// <summary>
    /// Filter by PM email
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? PMEmailId { get; set; }

    /// <summary>
    /// Filter by license type
    /// </summary>
    [Filterable]
    public string? LicenceType { get; set; }

    /// <summary>
    /// Filter by support type
    /// </summary>
    [Filterable]
    public string? SupportType { get; set; }
}
