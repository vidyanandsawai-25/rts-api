using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for Property filtering, sorting, and pagination.
/// Includes performance-optimized collection size limits.
/// </summary>
public class PropertyQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by PropertyId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }

    /// <summary>
    /// Filter by TaxZoneId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? TaxZoneId { get; set; }

    /// <summary>
    /// Filter by multiple WardIds (SQL IN clause).
    /// <para>Example: ?WardIds=1&amp;WardIds=2</para>
    /// </summary>
    [Filterable(FilterOperator.In, EntityProperty = "WardId")]
    public List<int>? WardIds { get; set; }

    /// <summary>
    /// Filter by single WardId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? WardId { get; set; }

    /// <summary>
    /// Search property number (case-insensitive contains).
    /// Example: ?PropertyNo=123
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Search partition number (case-insensitive contains).
    /// Example: ?PartitionNo=A
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Filter by PropertyTypeId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? PropertyTypeId { get; set; }

    /// <summary>
    /// Filter by CategoryId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Search by owner name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? OwnerName { get; set; }

    /// <summary>
    /// Filter by PartType (case-insensitive contains)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PartType { get; set; }

    /// <summary>
    /// Filter by Type (case-insensitive contains)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? Type { get; set; } 

    /// <summary>
    /// Filter by MarkedForDeletion status
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public bool? MarkedForDeletion { get; set; }


    /// <summary>
    /// Filter by IsActive status
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}

