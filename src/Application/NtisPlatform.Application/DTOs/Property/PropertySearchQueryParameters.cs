using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Enums;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for property search - supports Quick Search and KYC Search tabs
/// Inherits global pagination, sorting, and filtering from BaseQueryParameters
/// </summary>
public class PropertySearchQueryParameters : BaseQueryParameters
{
    // Dashboard Filter Parameters

    /// <summary>
    /// Filter by dashboard card clicked (Registered, Geo-Sequencing, Survey, etc.)
    /// </summary>
    public DashboardFilterType? DashboardFilter { get; set; }

    /// <summary>
    /// Filter by property process type from Type dropdown
    /// (Survey Completed, Data Entry Completed, QC Completed, Notice Distributed)
    /// </summary>
    public PropertyProcessFilterType? PropertyProcessFilter { get; set; }

    // Quick Search Tab Parameters

    /// <summary>
    /// Filter by Property Type dropdown
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? PropertyTypeId { get; set; }

    /// <summary>
    /// Filter by Property Description (Type of Use)
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? TypeOfUseId { get; set; }

    /// <summary>
    /// Filter by TaxZoneId (Zone dropdown)
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? ZoneId { get; set; }

    /// <summary>
    /// Filter by WardId (Ward dropdown)
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? WardId { get; set; }

    /// <summary>
    /// Filter by Property Category
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Filter by PropertyNo - From Property
    /// </summary>
    [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "PropertyNo")]
    public string? PropertyNoFrom { get; set; }

    /// <summary>
    /// Filter by PropertyNo - To Property
    /// </summary>
    [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "PropertyNo")]
    public string? PropertyNoTo { get; set; }

    /// <summary>
    /// Filter by OldPropertyNo from PropertyMastOld table
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? OldPropertyNo { get; set; }

    /// <summary>
    /// Filter by UPICId (UPIC Address)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? UPICId { get; set; }

    /// <summary>
    /// Filter by CSN (City Survey No)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? CSN { get; set; }

    /// <summary>
    /// Filter by SubZoneNo
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? SubZoneNo { get; set; }

    /// <summary>
    /// Filter by PlotNo
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? PlotNo { get; set; }

    /// <summary>
    /// Filter by PropertyAssessmentStatusId
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    public int? PropertyAssessmentStatusId { get; set; }

    // KYC Search Tab Parameters

    /// <summary>
    /// Search by Mobile Number
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? MobileNo { get; set; }

    /// <summary>
    /// Search by Property Holder Name (OwnerName)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? OwnerName { get; set; }

    /// <summary>
    /// Search by Occupier Name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? OccupierName { get; set; }

    /// <summary>
    /// Search by Shop/Building Name (FlatOrShopName)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? FlatOrShopName { get; set; }

    /// <summary>
    /// Search by Society Name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? SocietyName { get; set; }

    /// <summary>
    /// Search by Address
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    public string? Address { get; set; }


    // Values & Dues Search Tab Parameters

    /// <summary>
    /// Filter by RV or CV type. Allowed values: RV, CV.
    /// </summary>
    public string? RVorCV { get; set; }

    /// <summary>
    /// Filter operator for Total Tax amount filtering.
    /// <para><b>Valid values for tax filtering:</b></para>
    /// <para>• <b>Equals</b> - Find properties with exact tax amount (requires AmountValue)</para>
    /// <para>• <b>GreaterThan</b> - Find properties with tax greater than amount (requires AmountValue)</para>
    /// <para>• <b>LessThan</b> - Find properties with tax less than amount (requires AmountValue)</para>
    /// <para>• <b>Between</b> - Find properties with tax between two amounts (requires AmountValue and AmountTo)</para>
    /// <para>• <b>Top</b> - Get top N properties with highest tax (requires TopCount, ignores AmountValue)</para>
    /// <para><b>Example:</b> "Top" to get highest tax properties</para>
    /// </summary>
    /// <example>Top</example>
    public string? AmountFilterOperator { get; set; }

    /// <summary>
    /// Amount value used for Total Tax filtering.
    /// For Between filter, this is the starting amount.
    /// </summary>
    public decimal? AmountValue { get; set; }

    /// <summary>
    /// Ending amount used only when AmountFilterOperator is Between.
    /// </summary>
    public decimal? AmountTo { get; set; }

    /// <summary>
    /// Number of top properties to return when AmountFilterOperator is Top.
    /// Returns properties with highest total tax values.
    /// </summary>
    public int? TopCount { get; set; }
}
