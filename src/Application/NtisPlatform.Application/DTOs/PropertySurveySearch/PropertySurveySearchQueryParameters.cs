using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.PropertySurveySearch;

/// <summary>
/// Query parameters for Property Survey Search.
/// </summary>
public class PropertySurveySearchQueryParameters
    : BaseQueryParameters
{
    /// <summary>
    /// UI-selected ward number, such as W8, KH1 or UT1.
    /// </summary>
    [Required(ErrorMessage = "WardNo is required.")]
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? WardNo { get; set; }

    /// <summary>
    /// Property source status: NEW or OLD.
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string Status { get; set; } = "NEW";

    /// <summary>
    /// Property type filter: ALL or APARTMENT.
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public string PropertyType { get; set; } = "ALL";

    /// <summary>
    /// Logged-in user's ID.
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? UserId { get; set; }

    /// <summary>
    /// General search text.
    /// </summary>
    [Searchable]
    public string? SearchText { get; set; }

    /// <summary>
    /// Legacy alternative to SearchText.
    /// </summary>
    [Searchable]
    public string? SearchTerm { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PropertyNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PartitionNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? OwnerName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? MobileNo { get; set; }

    [Filterable(FilterOperator.Equals)]
    public int? PropertyTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public int? CategoryId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? UPICId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PartType { get; set; }
}