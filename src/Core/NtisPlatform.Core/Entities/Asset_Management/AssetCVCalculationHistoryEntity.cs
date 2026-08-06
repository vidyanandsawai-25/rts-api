using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management
{
    /// <summary>
    /// Represents the capital value calculation audit history trail for an asset in the AMS schema
    /// </summary>
    public class AssetCVCalculationHistoryEntity : BaseEntity, IHardDeletable
    {
        public int AssetId { get; set; }
        public DateTime CalculationDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public int? SubZoneId { get; set; }
        public int? FloorId { get; set; }
        public int? ConstructionTypeId { get; set; }
        public int? TypeOfUseId { get; set; }
        public int? SubTypeOfUseId { get; set; }
        public int? ConstructionYear { get; set; }
        public int? BuildingAge { get; set; }
        public decimal? BuiltUpAreaSqMeter { get; set; }
        public decimal? BaseRate { get; set; }
        public decimal? AgeFactor { get; set; }
        public decimal? FloorFactor { get; set; }
        public decimal? NatureFactor { get; set; }
        public decimal? UseFactor { get; set; }
        public decimal CapitalValue { get; set; }
        public int? CalculatedBy { get; set; }
        public string? Remarks { get; set; }

        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }

        // Navigation property (no virtual keyword per Guidelines.cs)
        public AssetMasterEntity? AssetMaster { get; set; }
    }
}
