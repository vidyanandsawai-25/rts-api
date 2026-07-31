namespace NtisPlatform.Application.DTOs.Asset_Management.CVCalculation;

/// <summary>
/// DTO for Capital Value calculation result
/// </summary>
public class CVCalculationResultDto
{
    public decimal CapitalValue { get; set; }
    public decimal BaseValue { get; set; }
    public decimal Rate { get; set; }
    public decimal CarpetAreaSqMeter { get; set; }
    public decimal NatureFactor { get; set; }
    public decimal UseFactor { get; set; }
    public decimal AgeFactor { get; set; }
    public decimal FloorFactor { get; set; }
    public int AgeOfAsset { get; set; }
    public string? CalculationDetails { get; set; }
}

/// <summary>
/// Input DTO for CV calculation
/// </summary>
public class CVCalculationInputDto
{
    public int AssetId { get; set; }
    public int? SubZoneId { get; set; }
    public int? TypeOfUseId { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public int? ConstructionTypeId { get; set; }
    public int? FloorId { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public int? ConstructionYear { get; set; }
    public int? AssessmentYear { get; set; }
    public bool HasLift { get; set; }
}
