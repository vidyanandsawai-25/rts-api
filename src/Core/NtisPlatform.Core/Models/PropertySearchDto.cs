using NtisPlatform.Core.Enums;

namespace NtisPlatform.Core.Models;

/// <summary>
/// Request DTO for property search filters used by the Quick Search and KYC Search tabs.
/// </summary>
public class PropertySearchRequestDto
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
    /// Filter by Property Type
    /// </summary>
    public int? PropertyTypeId { get; set; }

    /// <summary>
    /// Filter by Type of Use (Property Description)
    /// </summary>
    public int? TypeOfUseId { get; set; }

    /// <summary>
    /// Filter by TaxZoneId (Zone dropdown)
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Filter by WardId (Ward dropdown)
    /// </summary>
    public int? WardId { get; set; }

    /// <summary>
    /// Filter by Property Category
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Filter by PropertyNo - From Property
    /// </summary>
    public string? PropertyNoFrom { get; set; }

    /// <summary>
    /// Filter by PropertyNo - To Property
    /// </summary>
    public string? PropertyNoTo { get; set; }

    /// <summary>
    /// Filter by OldPropertyNo from PropertyMastOld table
    /// </summary>
    public string? OldPropertyNo { get; set; }

    /// <summary>
    /// Filter by UPICId (UPIC Address)
    /// </summary>
    public string? UPICId { get; set; }

    /// <summary>
    /// Filter by CSN (City Survey No)
    /// </summary>
    public string? CSN { get; set; }

    /// <summary>
    /// Filter by SubZoneNo
    /// </summary>
    public string? SubZoneNo { get; set; }

    /// <summary>
    /// Filter by PlotNo
    /// </summary>
    public string? PlotNo { get; set; }

    /// <summary>
    /// Filter by PropertyAssessmentStatusId
    /// </summary>
    public int? PropertyAssessmentStatusId { get; set; }

    // KYC Search Tab Parameters

    /// <summary>
    /// Search by Mobile Number
    /// </summary>
    public string? MobileNo { get; set; }

    /// <summary>
    /// Search by Property Holder Name (OwnerName)
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Search by Occupier Name
    /// </summary>
    public string? OccupierName { get; set; }

    /// <summary>
    /// Search by Shop/Building Name (FlatOrShopName)
    /// </summary>
    public string? FlatOrShopName { get; set; }

    /// <summary>
    /// Search by Society Name
    /// </summary>
    public string? SocietyName { get; set; }

    /// <summary>
    /// Search by Address
    /// </summary>
    public string? Address { get; set; }


    // Values & Dues Search Tab Parameters

    /// <summary>
    /// Filter by RV or CV type. Allowed values: RV, CV.
    /// </summary>
    public string? RVorCV { get; set; }

    /// <summary>
    /// Filter operator for calculated Total Tax amount.
    /// Supported values: Equals, GreaterThan, LessThan, Between.
    /// </summary>
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
}

/// <summary>
/// Response DTO for property search results
/// </summary>
public class PropertySearchResponseDto
{
    public int PropertyId { get; set; }
    public string? UPICId { get; set; }
    public string? ZoneName { get; set; }
    public string? WardName { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? CitySurveyNo { get; set; }
    public string? PlotNo { get; set; }
    public string? WingFlatNo { get; set; }
    public string? CategoryName { get; set; }
    public string? PropertyDescription { get; set; }
    public string? Mobile { get; set; }
    public string? PropertyHolderName { get; set; }
    public string? OccupierName { get; set; }
    public string? ShopBuildingName { get; set; }
    public string? SocietyName { get; set; }
    public string? Address { get; set; }
    public decimal? RV { get; set; }
    public decimal? CV { get; set; }
    public decimal? TotalTax { get; set; }
}
