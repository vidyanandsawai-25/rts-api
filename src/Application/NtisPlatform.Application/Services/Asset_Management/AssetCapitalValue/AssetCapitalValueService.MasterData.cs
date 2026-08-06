using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services
{
    public partial class AssetCapitalValueService
    {
        #region Helper Classes

        private class CVMasterData
        {
            public List<AssetAssessmentYearRangeMasterCVEntity> YearRanges { get; set; } = new();
            public List<AssetFloorFactorCVEntity> FloorFactors { get; set; } = new();
            public List<AssetNatureFactorCVMasterEntity> NatureFactors { get; set; } = new();
            public List<AssetUseFactorCVMasterEntity> UseFactors { get; set; } = new();
            public List<AssetAgeFactorCVMasterEntity> AgeFactors { get; set; } = new();
            public List<AssetTypeOfUseMasterEntity> TypeOfUses { get; set; } = new();
            public List<AssetTypeOfUseGroupEntity> TypeOfUseGroups { get; set; } = new();
            public List<RateMasterLookup> AllRateMasters { get; set; } = new();

            // --- Precomputed lookups (built once per request in LoadMasterDataAsync) ---
            // Each of these turns a per-floor-detail O(n) FirstOrDefault scan over the lists above
            // into an O(1) (or, for AgeFactors, small-bucket O(k)) dictionary lookup. See
            // LoadMasterDataAsync for how each is built and why it reproduces the original
            // FirstOrDefault's first-match semantics exactly.
            public Dictionary<int, AssetTypeOfUseMasterEntity> TypeOfUseById { get; set; } = new();
            public Dictionary<int, AssetTypeOfUseGroupEntity> TypeOfUseGroupById { get; set; } = new();
            public Dictionary<(int? YearRangeId, int? TypeOfUseGroupId, int? FloorGroupId), RateMasterLookup> RateByYearRangeUseGroupFloorGroup { get; set; } = new();
            public Dictionary<(int? YearRangeId, int? TypeOfUseGroupId), RateMasterLookup> RateByYearRangeUseGroup { get; set; } = new();
            public Dictionary<(int ConstructionTypeId, int YearRangeCVId), decimal> NatureFactorByConstructionTypeAndYearRange { get; set; } = new();
            public Dictionary<(int TypeOfUseId, int YearRangeCVId, int SubTypeOfUseId), decimal> UseFactorByTypeOfUseYearRangeAndSubType { get; set; } = new();
            public Dictionary<(int ConstructionTypeId, int YearRangeCVId), List<AssetAgeFactorCVMasterEntity>> AgeFactorsByConstructionTypeAndYearRange { get; set; } = new();
            public Dictionary<(int FloorId, int YearRangeCVId), AssetFloorFactorCVEntity> FloorFactorByFloorAndYearRange { get; set; } = new();
        }

        private class RateMasterLookup
        {
            public int Id { get; set; }
            public int? SubZoneId { get; set; }
            public int? TypeOfUseGroupCVId { get; set; }
            public int? FloorGroupId { get; set; }
            public int? AssessmentYearRangeId { get; set; }
            public decimal? RateAmount { get; set; }
        }

        public static class CapitalValueCalculationEngine
        {
            public static (decimal CapitalValue, decimal BaseValue, string Formula) Calculate(
                decimal baseRate,
                decimal carpetAreaSqMeter,
                decimal natureFactor = 1m,
                decimal useFactor = 1m,
                decimal ageFactor = 1m,
                decimal floorFactor = 1m)
            {
                if (carpetAreaSqMeter <= 0) return (0, 0, "Invalid carpet area");

                decimal baseValue = baseRate * carpetAreaSqMeter;
                decimal capitalValue = baseValue * natureFactor * useFactor * ageFactor * floorFactor;

                string formula = $"CV = ({baseRate:F2} × {carpetAreaSqMeter:F4}) × {natureFactor:F4} × {useFactor:F4} × {ageFactor:F4} × {floorFactor:F4} = {capitalValue:F2}";

                return (capitalValue, baseValue, formula);
            }
        }

        #endregion
    }
}
