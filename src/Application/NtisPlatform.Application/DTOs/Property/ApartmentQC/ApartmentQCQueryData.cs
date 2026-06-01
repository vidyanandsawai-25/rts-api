namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

// ──────────────────────────────────────────────────────────────────────────────
// Boundary types that cross the Infrastructure → Application layer.
// The Infrastructure repository materialises EF LINQ projections into these
// plain types, which the Application service then assembles into API DTOs.
// ──────────────────────────────────────────────────────────────────────────────

public sealed class ApartmentQCPropertyData
{
    public int     Id                    { get; init; }
    public int?    TaxZoneId             { get; init; }
    public int     WardId                { get; init; }
    public string? PropertyNo            { get; init; }
    public string? PartitionNo           { get; init; }
    public string? MobileNo              { get; init; }
    public string? EmailId               { get; init; }
    public string? FlatOrShopNo          { get; init; }
    public string? FlatOrShopName        { get; init; }
    public string? FlatOrShopNoEnglish   { get; init; }
    public string? FlatOrShopNameEnglish { get; init; }
    public string? OwnerName             { get; init; }
    public string? OwnerNameEnglish      { get; init; }
    public string? OccupierName          { get; init; }
    public string? OccupierNameEnglish   { get; init; }
    public string? PartType              { get; init; }
    public int?    PropertyType          { get; init; }
    public string? PropertyTypeName      { get; init; }
    public string? BHK                  { get; init; }
    public string? Wing                 { get; init; }
    public string? ApartmentType        { get; init; }
}

public sealed class ApartmentQCDetailData
{
    public int      Id                 { get; init; }
    public int      PropertyId         { get; init; }
    public string?  ConstructionYear   { get; init; }
    public string?  AssessmentYear     { get; init; }
    public decimal? CarpetAreaSqMeter  { get; init; }
    public decimal? CarpetAreaSqFeet   { get; init; }
    public decimal? BuiltupAreaSqMeter { get; init; }
    public decimal? BuiltupAreaSqFeet  { get; init; }
    public string?  Floor              { get; init; }
    public string?  SubFloor           { get; init; }
    public string?  TypeOfUse          { get; init; }
    public string?  Type               { get; init; }
    public string?  ConstructionType   { get; init; }
    public string?  SubTypeOfUse       { get; init; }
    public int?     NoOfRooms          { get; init; }
}

public sealed record ApartmentQCOldPropertyData(
    int      Id,
    string?  OldPropertyNo,
    decimal? OldConstructionArea,
    decimal? OldRV,
    decimal? OldTotalTax,
    string?  OldUseType,
    string?  OldConstructionYear,
    string?  OldConstructionType,
    string?  OldCSN);

public sealed record ApartmentQCWardData(int Id, string? WardNo, string? ZoneNo);

public sealed record ApartmentQCOccupancyData(int PropertyDetailId, DateTime? OccupancyDate);

public sealed record ApartmentQCRenterData(
    int      PropertyDetailsId,
    string?  RenterName,
    string?  RenterNameEnglish,
    decimal? FinalYearlyRent,
    decimal? RentMonthly);

public sealed record ApartmentQCTransactionData(int PropertyId, decimal? RVorCVValue, decimal TmTaxAmount);

public sealed record ApartmentQCTransactionCVData(int PropertyId, decimal? CapitalValue, decimal TmcvTaxAmount);

public sealed record ApartmentQCTransactionRVData(int PropertyId, decimal? RateableValue, decimal TmrvTaxAmount);

public sealed record ApartmentQCTaxPendingData(int PropertyId, decimal PendingAmount);

public sealed record ApartmentQCRvCalcData(
    int      PropertyDetailsId,
    decimal? YearlyRent,
    decimal? MonthlyRate,
    decimal? YearlyRate,
    decimal? Depreciation,
    decimal? AnnualRentalValue,
    decimal? Maintenance,
    decimal? RateableValue);

public sealed record ApartmentQCCvCalcData(
    int      PropertyDetailsId,
    decimal? BaseValue,
    decimal? FloorFactor,
    decimal? AgeFactor,
    decimal? NatureFactor,
    decimal? UseFactor,
    decimal? CapitalValue,
    int?     FloorFactorId,
    int?     AgeFactorId,
    int?     NatureFactorId,
    int?     UseFactorId,
    decimal? SDRR);

/// <summary>
/// Container returned by the repository fetch methods.
/// The Application service assembles the data inside this container into
/// <see cref="ApartmentQCRawData"/> items and ultimately into API response DTOs.
/// </summary>
public sealed class ApartmentQCFetchedData
{
    public static readonly ApartmentQCFetchedData Empty = new();

    public IReadOnlyList<ApartmentQCPropertyData>                        Properties  { get; init; } = Array.Empty<ApartmentQCPropertyData>();
    public IReadOnlyDictionary<int, ApartmentQCOldPropertyData>          OldData     { get; init; } = new Dictionary<int, ApartmentQCOldPropertyData>();
    public IReadOnlyDictionary<int, ApartmentQCWardData>                 WardZones   { get; init; } = new Dictionary<int, ApartmentQCWardData>();
    public IReadOnlyList<ApartmentQCDetailData>                          Details     { get; init; } = Array.Empty<ApartmentQCDetailData>();
    public IReadOnlyDictionary<int, ApartmentQCOccupancyData>            Occupancies { get; init; } = new Dictionary<int, ApartmentQCOccupancyData>();
    public IReadOnlyDictionary<int, ApartmentQCRenterData>               Renters     { get; init; } = new Dictionary<int, ApartmentQCRenterData>();
    public IReadOnlyDictionary<int, ApartmentQCTransactionData>          Tm          { get; init; } = new Dictionary<int, ApartmentQCTransactionData>();
    public IReadOnlyDictionary<int, ApartmentQCTransactionCVData>        Tmcv        { get; init; } = new Dictionary<int, ApartmentQCTransactionCVData>();
    public IReadOnlyDictionary<int, ApartmentQCTransactionRVData>        Tmrv        { get; init; } = new Dictionary<int, ApartmentQCTransactionRVData>();
    public IReadOnlyDictionary<int, ApartmentQCTaxPendingData>           Tp          { get; init; } = new Dictionary<int, ApartmentQCTaxPendingData>();
    public IReadOnlyDictionary<int, ApartmentQCTaxPendingData>           Tpcv        { get; init; } = new Dictionary<int, ApartmentQCTaxPendingData>();
    public IReadOnlyDictionary<int, ApartmentQCTaxPendingData>           Tprv        { get; init; } = new Dictionary<int, ApartmentQCTaxPendingData>();
    public IReadOnlyDictionary<int, ApartmentQCRvCalcData>               RvCalc      { get; init; } = new Dictionary<int, ApartmentQCRvCalcData>();
    public IReadOnlyDictionary<int, ApartmentQCCvCalcData>               CvCalc      { get; init; } = new Dictionary<int, ApartmentQCCvCalcData>();
}
