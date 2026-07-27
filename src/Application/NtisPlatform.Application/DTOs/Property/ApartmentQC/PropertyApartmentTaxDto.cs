namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// API response DTO for both the aggregated (per-property) and expanded
/// (per-PropertyDetails) Apartment QC endpoints.
/// </summary>
public class PropertyApartmentTaxDto
{
    public long Id { get; set; }
    public long? PDNId { get; set; }
    public int? TaxZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string? OldPropertyNo { get; set; }
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
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
    public int?    NoOfRooms { get; set; }
    public string? Floor { get; set; }
    public string? SubFloor { get; set; }
    public string? SubTypeOfUse { get; set; }

    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public string? ConstructionType { get; set; }

    public decimal? OldConstructionArea { get; set; }
    public string? OldConstructionYear { get; set; }
    public string? OldUseType { get; set; }
    public string? OldConstructionType { get; set; }
    public decimal? OldRV { get; set; }
    public decimal? OldTotalTax { get; set; }
    public string?  OldCSN { get; set; }

    public decimal? CalculationValue { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? RateableValue { get; set; }

    public decimal NewTaxTotal { get; set; }
    public decimal NewTaxTotalCV { get; set; }
    public decimal NewTaxTotalRV { get; set; }

    // RVCalculationResults — populated when ResultType is Rateable or Dual
    public decimal? YearlyRent { get; set; }
    public decimal? MonthlyRate { get; set; }
    public decimal? YearlyRate { get; set; }
    public decimal? Depreciation { get; set; }
    public decimal? AnnualRentalValue { get; set; }
    public decimal? Maintenance { get; set; }

    // PropertyTaxCalculationCVResults — populated when ResultType is Capital or Dual
    public decimal? SDRR { get; set; }
    public decimal? BaseValue { get; set; }
    public decimal? FloorFactor { get; set; }
    public decimal? AgeFactor { get; set; }
    public decimal? NatureFactor { get; set; }
    public decimal? UseFactor { get; set; }

    // CV factor master IDs — returned alongside values so the frontend can pre-select
    // the current master entry in edit-form dropdowns; null in the aggregated endpoint.
    public int? FloorFactorId { get; set; }
    public int? AgeFactorId { get; set; }
    public int? NatureFactorId { get; set; }
    public int? UseFactorId { get; set; }

    public decimal? CarpetASqMtr { get; set; }
    public decimal? CarpetASqFt { get; set; }
    public decimal? BuiltupASqMtr { get; set; }
    public decimal? BuiltupASqFt { get; set; }
}
