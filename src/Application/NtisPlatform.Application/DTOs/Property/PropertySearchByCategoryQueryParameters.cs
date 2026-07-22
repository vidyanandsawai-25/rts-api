using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Enums;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for PropertySearchByCategory - a scoped property search selected by
/// <see cref="PropertySearchCategory"/> (Zone-wise, Ward-wise, Building-wise, or a From/To
/// property-number range within a ward).
/// Inherits global pagination and the SearchTerm free-text filter from BaseQueryParameters -
/// SearchTerm matches WardNo, PropertyNo, or PartitionNo (any match), applied server-side
/// alongside the SearchCategory scope so results stay SQL-paginated.
/// </summary>
public class PropertySearchByCategoryQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Selects which of the fields below are required. 1=ZoneWise, 2=WardWise, 3=BuildingWise, 4=FromToProperty.
    /// </summary>
    public PropertySearchCategory SearchCategory { get; set; }

    /// <summary>
    /// Required when SearchCategory is ZoneWise.
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Required when SearchCategory is WardWise, BuildingWise, or FromToProperty.
    /// </summary>
    public int? WardId { get; set; }

    /// <summary>
    /// Required when SearchCategory is BuildingWise.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional refinement for BuildingWise.
    /// </summary>
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Required when SearchCategory is FromToProperty. Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1-A9").
    /// </summary>
    public string? PropertyFrom { get; set; }

    /// <summary>
    /// Optional upper bound for FromToProperty. When omitted, the range is open-ended from PropertyFrom onward.
    /// Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1-S2").
    /// </summary>
    public string? PropertyTo { get; set; }

    /// <summary>
    /// Optional filter by PropertyTypeMaster.PartType. Comma-separated for multiple values
    /// (e.g. "R,C,Amenity").
    /// </summary>
    [Filterable(FilterOperator.In)]
    [Searchable]
    public string? PartType { get; set; }

    /// <summary>
    /// Optional filter by PropertyCategoryMaster.PropertyCategoryName. Comma-separated for
    /// multiple values (e.g. "Apartment,Individual").
    /// </summary>
    [Filterable(FilterOperator.In)]
    [Searchable]
    public string? PropertyCategoryName { get; set; }

    /// <summary>
    /// Optional filter by PropertyAssessmentStatusId. Comma-separated for multiple values (e.g. "1,2").
    /// </summary>
    [Filterable(FilterOperator.In)]
    public string? PropertyAssessmentStatusId { get; set; }

    /// <summary>
    /// Optional filter by the computed IsWing flag (whether PartitionNo matches a WingMaster.WingNo).
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public bool? IsWing { get; set; }
}
