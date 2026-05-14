using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Query parameters for CombineProperty listing with filtering, sorting, and pagination
/// </summary>
public class CombinePropertyQueryParameters : BaseQueryParameters
{

    [Filterable]
    [Searchable]
    [Sortable]
    public int? WardId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PropertyNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PartitionNo { get; set; }
}

/// <summary>
/// Query parameters for GetPropertyCombineDetails endpoint
/// </summary>
public class PropertyCombineDetailsQueryParameters : BaseQueryParameters
{

    [Filterable]
    [Required(ErrorMessage = "CombineProperty_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_WardId_Invalid")]
    public int? WardId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Required(ErrorMessage = "CombineProperty_PropertyNo_Required")]
    [StringLength(10, ErrorMessage = "CombineProperty_PropertyNo_MaxLen_10")]
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Comma-separated partition numbers (e.g., "1,2,3")
    /// </summary>
    [Required(ErrorMessage = "CombineProperty_PartitionNo_Required")]
    public string? PartitionNo { get; set; }
}