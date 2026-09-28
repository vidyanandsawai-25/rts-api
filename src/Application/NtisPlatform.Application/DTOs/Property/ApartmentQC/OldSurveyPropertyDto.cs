namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Details from the mapped Old Survey property for Apartment QC comparison.
/// </summary>
public class OldSurveyPropertyDto
{
    public long Id { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string? OldPropertyNo { get; set; }
    public string? WardNo { get; set; }
    public string? ZoneNo { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? OCNo { get; set; }
    public DateTime? OCDate { get; set; }

    public int? OldPropertyTypeId { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldEgovNo { get; set; }
    public string? OldPlotNo { get; set; }
    public int? OldAssessmentYear { get; set; }
    public DateTime? OldAssessmentDate { get; set; }
    public string? OldConstructionTypeOfUseId { get; set; }

    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopName { get; set; }

    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }

    public string? Floor { get; set; }
    public string? Wing { get; set; }
    public int? NoOfRooms { get; set; }

    public string? ConstructionYear { get; set; }
    public string? TypeOfUse { get; set; }
    public string? ConstructionType { get; set; }

    public decimal? RateableValue { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? RetroTaxTotal { get; set; }
    public string? CSN { get; set; }

    public decimal? ConstructionArea { get; set; }
    public decimal? CarpetASqMtr { get; set; }
    public decimal? CarpetASqFt { get; set; }
    public decimal? BuiltupASqMtr { get; set; }
    public decimal? BuiltupASqFt { get; set; }

    public Guid? PropertyPhotoDocumentGuid { get; set; }
    public Guid? PlanPhotoDocumentGuid { get; set; }
    public List<PropertyPhotoDocumentDto> Photos { get; set; } = new();
    public List<OldTaxDetailDto> OldTaxDetails { get; set; } = new();
}

/// <summary>
/// Details of historical tax transaction from TransMastOld for Old Survey property.
/// </summary>
public class OldTaxDetailDto
{
    public int Id { get; set; }
    public int PropertyMastOldId { get; set; }
    public int FinanceYearId { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public decimal? CalculationValue { get; set; }
    public decimal? CalculationAnnualValue { get; set; }
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal TaxAmount { get; set; }
}
