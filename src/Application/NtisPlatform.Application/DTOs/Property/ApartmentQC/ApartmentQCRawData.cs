namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Internal data-transfer record passed from <c>ApartmentQCRepository</c> to
/// <c>ApartmentQCService</c>. Never exposed directly to API consumers.
/// </summary>
public sealed record ApartmentQCRawData
{
    public long Id { get; init; }
    public long? PDNId { get; init; }
    public int? TaxZoneId { get; init; }
    public int? WardId { get; init; }
    public string? WardNo { get; init; }
    public string? ZoneNo { get; init; }
    public string? RawPropertyNo { get; init; }
    public string? PartitionNo { get; init; }
    public string? MobileNo { get; init; }
    public string? EmailId { get; init; }

    public string? FlatOrShopNo { get; init; }
    public string? FlatOrShopName { get; init; }
    public string? FlatOrShopNoEnglish { get; init; }
    public string? FlatOrShopNameEnglish { get; init; }

    public string? OwnerName { get; init; }
    public string? OwnerNameEnglish { get; init; }
    public string? OccupierName { get; init; }
    public string? OccupierNameEnglish { get; init; }

    public string? PartType { get; init; }
    public int? PropertyType { get; init; }
    public string? PropertyTypeName { get; init; }
    public string? BHK { get; init; }
    public string? Wing { get; init; }
    public string? ApartmentType { get; init; }

    public string? OldPropertyNo { get; init; }
    public decimal? OldConstructionArea { get; init; }
    public string? OldConstructionYear { get; init; }
    public string? OldUseType { get; init; }
    public string? OldConstructionType { get; init; }
    public decimal? OldRV { get; init; }
    public decimal? OldTotalTax { get; init; }
    public string? OldCSN { get; init; }

    public string? RenterName { get; init; }
    public string? RenterNameEnglish { get; init; }
    public decimal? RentYearly { get; init; }
    public decimal? RentMonthly { get; init; }

    public decimal CarpetAreaSqMeter { get; init; }
    public decimal CarpetAreaSqFeet { get; init; }
    public decimal BuiltupAreaSqMeter { get; init; }
    public decimal BuiltupAreaSqFeet { get; init; }

    public int? NoOfRooms { get; init; }

    public IReadOnlyCollection<string> Floors { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SubFloors { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> TypesOfUse { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Types { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ConstructionTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ConstructionYears { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> AssessmentYears { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SubTypesOfUse { get; init; } = Array.Empty<string>();

    // RV calculation fields (null when ResultType is Capital)
    public decimal? CalcYearlyRent { get; init; }
    public decimal? CalcMonthlyRate { get; init; }
    public decimal? CalcYearlyRate { get; init; }
    public decimal? CalcDepreciation { get; init; }
    public decimal? CalcAnnualRentalValue { get; init; }
    public decimal? CalcMaintenance { get; init; }

    // CV calculation fields (null when ResultType is Rateable)
    public decimal? CalcSDRR { get; init; }
    public decimal? CalcBaseValue { get; init; }
    public decimal? CalcFloorFactor { get; init; }
    public decimal? CalcAgeFactor { get; init; }
    public decimal? CalcNatureFactor { get; init; }
    public decimal? CalcUseFactor { get; init; }

    // CV factor master IDs — returned so the frontend can pre-select the correct
    // dropdown option when editing; only populated by GetByPropertyDetailsAsync.
    public int? FloorFactorId { get; init; }
    public int? AgeFactorId { get; init; }
    public int? NatureFactorId { get; init; }
    public int? UseFactorId { get; init; }

    public DateTime? OCDate { get; init; }

    public decimal? CalculationValue { get; init; }
    public decimal? CapitalValue { get; init; }
    public decimal? RateableValue { get; init; }

    public decimal TmTaxAmount { get; init; }
    public decimal TmcvTaxAmount { get; init; }
    public decimal TmrvTaxAmount { get; init; }
    public decimal TpPendingAmount { get; init; }
    public decimal TpcvPendingAmount { get; init; }
    public decimal TprvPendingAmount { get; init; }
}
