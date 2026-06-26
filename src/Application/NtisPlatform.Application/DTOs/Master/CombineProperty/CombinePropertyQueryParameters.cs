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

    /// <summary>
    /// Category ID for filtering. When set to Apartment or Multi Commercial Apartment,
    /// filters by both WardId and PropertyNo. For non-apartment, filters by WardId only.
    /// </summary>
    [Filterable]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Society Detail ID for filtering apartments by wing.
    /// Only applicable for apartment categories to show properties from the same wing.
    /// Ignored for non-apartment categories.
    /// </summary>
    [Filterable]
    public int? SocietyDetailId { get; set; }
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

    /// <summary>
    /// Optional comma-separated property numbers (e.g., "1,2,3")
    /// </summary>
    [Filterable(FilterOperator.Contains)]  
    [StringLength(100, ErrorMessage = "CombineProperty_PropertyNo_MaxLen_100")]
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional comma-separated partition numbers (e.g., "A,B,C")
    /// </summary>
    public string? PartitionNo { get; set; }
}