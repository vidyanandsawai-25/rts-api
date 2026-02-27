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
    /// Filter by multiple wards (SQL IN clause).
    /// Example: ?WardNos=WKD1&WardNos=MSH1
    /// Recommended maximum: 500 items for optimal SQL Server performance.
    /// For larger datasets, consider using range filters or multiple API calls.
    /// </summary>
    [Filterable(FilterOperator.In, EntityProperty = "WardNo")]
    public List<string>? WardNos { get; set; }

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
    /// Minimum property number for range filtering (numeric-aware comparison).
    /// Example: ?MinPropertyNo=100 matches "100", "150", "200" but not "50"
    /// Note: Uses length-based comparison for numeric strings: "2" &lt; "10" &lt; "100"
    /// </summary>
    [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "PropertyNo")]
    public string? MinPropertyNo { get; set; }

    /// <summary>
    /// Maximum property number for range filtering (numeric-aware comparison).
    /// Example: ?MaxPropertyNo=200 matches "100", "150", "200" but not "250"
    /// Note: Uses length-based comparison for numeric strings: "2" &lt; "10" &lt; "100"
    /// </summary>
    [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "PropertyNo")]
    public string? MaxPropertyNo { get; set; }
}
