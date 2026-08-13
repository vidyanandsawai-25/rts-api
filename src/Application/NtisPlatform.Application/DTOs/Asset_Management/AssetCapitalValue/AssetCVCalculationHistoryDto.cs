using System;

namespace NtisPlatform.Application.DTOs.AssetCapitalValue
{
    /// <summary>
    /// Represents the DTO for retrieving a capital value calculation audit history entry
    /// </summary>
    public class AssetCVCalculationHistoryDto
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetNo { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public DateTime CalculationDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public int? SubZoneId { get; set; }
        public int? FloorId { get; set; }
        public string? FloorDescription { get; set; }
        public int? ConstructionTypeId { get; set; }
        public string? ConstructionTypeDescription { get; set; }
        public int? TypeOfUseId { get; set; }
        public string? TypeOfUseDescription { get; set; }
        public int? SubTypeOfUseId { get; set; }
        public string? SubTypeOfUseDescription { get; set; }
        public int? ConstructionYear { get; set; }
        public int? BuildingAge { get; set; }
        public decimal? BuiltUpAreaSqMeter { get; set; }
        public bool? HasLift { get; set; }
        public decimal? BaseRate { get; set; }
        public decimal? AgeFactor { get; set; }
        public decimal? FloorFactor { get; set; }
        public decimal? NatureFactor { get; set; }
        public decimal? UseFactor { get; set; }
        public decimal CapitalValue { get; set; }
        public string? CalculationFormula { get; set; }
        public int? CalculatedBy { get; set; }
        public string? Remarks { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
