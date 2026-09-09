namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Details from the New Survey property for Apartment QC comparison.
/// </summary>
public class NewSurveyPropertyDto
{
    public long Id { get; set; }
    public long? PDNId { get; set; }
    public int? TaxZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? OCNo { get; set; }
    public DateTime? OCDate { get; set; }

    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }

    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public int? PropertyType { get; set; }
    public string? PropertyTypeName { get; set; }

    public decimal? RentYearly { get; set; }
    public decimal? RentMonthly { get; set; }
    public string? RenterName { get; set; }
    public string? RenterNameEnglish { get; set; }

    public string? TypeOfUse { get; set; }
    public string? Type { get; set; }
    public string? ApartmentType { get; set; }
    public string? PartType { get; set; }
    public string? BHK { get; set; }
    public string? Wing { get; set; }
    public int? WingDetailId { get; set; }
    public int? NoOfRooms { get; set; }
    public string? Floor { get; set; }
    public string? SubFloor { get; set; }
    public string? SubTypeOfUse { get; set; }

    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public string? ConstructionType { get; set; }

    public decimal? CalculationValue { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? RateableValue { get; set; }

    public decimal NewTaxTotal { get; set; }
    public decimal NewTaxTotalCV { get; set; }
    public decimal NewTaxTotalRV { get; set; }
    public decimal? RetroTaxTotal { get; set; }
    public decimal? CurrentDemand { get; set; }

    public decimal? CarpetASqMtr { get; set; }
    public decimal? CarpetASqFt { get; set; }
    public decimal? BuiltupASqMtr { get; set; }
    public decimal? BuiltupASqFt { get; set; }

    public Guid? PropertyPhotoDocumentGuid { get; set; }
    public Guid? PlanPhotoDocumentGuid { get; set; }
    public List<PropertyPhotoDocumentDto> Photos { get; set; } = new();
}
