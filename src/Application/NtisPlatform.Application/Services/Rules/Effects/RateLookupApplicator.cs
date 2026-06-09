using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Rules.Effects
{
    /// <summary>
    /// Handles effectType "RateLookup" — looks up a rate from RateEntity master table 
    /// and applies a percentage/multiplier to it.
    /// 
    /// Automatically uses the current calculation context (TaxZoneId, ConstructionTypeId, YearRangeRVId)
    /// from the input dictionary, so you only need to specify:
    /// 
    /// Context structure:
    /// {
    ///   "effectType": "RateLookup",
    ///   "value": 50,  // Percentage to apply (50% of residential rate)
    ///   "RateTypeOfUseGroupId": 1  // REQUIRED: The TypeOfUseGroupId to lookup (e.g., 1 = Residential)
    /// }
    /// 
    /// Optional overrides (if you want to lookup a rate with different criteria):
    /// {
    ///   "RateFloorId": 1,  // Override: specific floor to lookup
    ///   "RateConstructionTypeId": 1,  // Override: specific construction type
    ///   "RateTaxZoneId": 1,  // Override: specific tax zone
    ///   "RateYearRangeRVId": 1  // Override: specific year range
    /// }
    /// </summary>
    public sealed class RateLookupApplicator : IRuleEffectApplicator
    {
        private readonly IRepository<RateEntity, int> _rateRepository;
        private readonly ILogger<RateLookupApplicator> _logger;

        // Store lookup context from the rule's Actions.OnSuccess.Context
        // This is set via a method before Apply is called
        private Dictionary<string, object>? _lookupContext;

        // Store input dictionary to extract TaxZoneId, ConstructionTypeId, etc.
        private Dictionary<string, object>? _inputDict;

        public RateLookupApplicator(
            IRepository<RateEntity, int> rateRepository,
            ILogger<RateLookupApplicator> logger)
        {
            _rateRepository = rateRepository;
            _logger = logger;
        }

        public bool CanHandle(string effectType) =>
            effectType.Contains("RateLookup", StringComparison.OrdinalIgnoreCase) ||
            effectType.Contains("Rate Lookup", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Sets the lookup context from the rule's Context JSON.
        /// Call this before Apply() to provide rate lookup parameters.
        /// </summary>
        public void SetLookupContext(Dictionary<string, object> context)
        {
            _lookupContext = context;
        }

        /// <summary>
        /// Sets the input dictionary to extract current calculation context.
        /// This provides TaxZoneId, ConstructionTypeId, YearRangeRVId, etc.
        /// </summary>
        public void SetInputDictionary(Dictionary<string, object> inputDict)
        {
            _inputDict = inputDict;
        }

        /// <summary>
        /// Looks up rate from master table and applies percentage.
        /// baseRate is ignored (we look up a new rate from the database).
        /// effectValue is the percentage/multiplier to apply to the looked-up rate.
        /// </summary>
        public async Task<decimal> Apply(decimal baseRate, decimal effectValue)
        {
            if (_lookupContext == null)
            {
                _logger.LogWarning(
                    "[RateLookupApplicator] No lookup context provided. Returning baseRate unchanged.");
                return baseRate;
            }

            try
            {
                // REQUIRED: RateTypeOfUseGroupId from rule context (which property type to lookup)
                var typeOfUseGroupId = GetContextInt(_lookupContext, "RateTypeOfUseGroupId");

                if (!typeOfUseGroupId.HasValue)
                {
                    _logger.LogWarning(
                        "[RateLookupApplicator] RateTypeOfUseGroupId is required but not provided. Returning baseRate.");
                    return baseRate;
                }

                // Extract from input dictionary (current calculation context) or allow rule to override
                // Priority: Rule context → Input dictionary → null
                // Note: Input dictionary uses different key names (e.g., "TaxZone" not "TaxZoneId")
                var taxZoneId = GetContextInt(_lookupContext, "RateTaxZoneId")
                    ?? GetContextInt(_inputDict, "TaxZone")  // Match actual key from context
                    ?? GetContextInt(_inputDict, "Zone");    // Alternate key

                var constructionTypeId = GetContextInt(_lookupContext, "RateConstructionTypeId")
                    ?? GetContextInt(_inputDict, "Construction Type");  // Match actual key (with space!)

                var yearRangeRVId = GetContextInt(_lookupContext, "RateYearRangeRVId")
                    ?? GetContextInt(_inputDict, "YearRangeRVId")
                    ?? GetContextInt(_inputDict, "FinanceYear");  // Use FinanceYear as fallback (needs lookup)

                // FloorId is typically NOT needed for rate lookup (rates are per TypeOfUse, not Floor)
                // But allow override if specified in rule
                var floorId = GetContextInt(_lookupContext, "RateFloorId")
                    ?? GetContextInt(_inputDict, "Floor");

                _logger.LogInformation(
                    "[RateLookupApplicator] Looking up rate: TypeOfUseGroupId={TypeOfUseGroupId}, " +
                    "TaxZoneId={TaxZoneId}, ConstructionTypeId={ConstructionTypeId}, YearRangeRVId={YearRangeRVId}",
                    typeOfUseGroupId, taxZoneId, constructionTypeId, yearRangeRVId);

                // Build query to lookup rate
                var query = _rateRepository.GetQueryable()
                    .Where(r => r.TypeOfUseGroupId == typeOfUseGroupId.Value && r.IsActive);

                // Apply optional filters
                if (floorId.HasValue)
                    query = query.Where(r => r.FloorId == floorId.Value);
                if (constructionTypeId.HasValue)
                    query = query.Where(r => r.ConstructionTypeId == constructionTypeId.Value);
                if (taxZoneId.HasValue)
                    query = query.Where(r => r.TaxZoneId == taxZoneId.Value);
                if (yearRangeRVId.HasValue)
                    query = query.Where(r => r.YearRangeRVId == yearRangeRVId.Value);

                // Execute query asynchronously to avoid blocking
                // Using AsNoTracking for read-only operation
                var rateEntity = await query.AsNoTracking().FirstOrDefaultAsync();

                if (rateEntity == null)
                {
                    _logger.LogWarning(
                        "[RateLookupApplicator] No rate found for TypeOfUseGroupId={TypeOfUseGroupId}, " +
                        "FloorId={FloorId}, ConstructionTypeId={ConstructionTypeId}, TaxZoneId={TaxZoneId}. " +
                        "Returning baseRate unchanged.",
                        typeOfUseGroupId, floorId, constructionTypeId, taxZoneId);
                    return baseRate;
                }

                // Use RateSquareMeter if available, fallback to RateSquareFeet
                var lookedUpRate = rateEntity.RateSquareMeter ??
                                   (rateEntity.RateSquareFeet.HasValue
                                       ? rateEntity.RateSquareFeet.Value * 10.764m  // Convert sq.ft to sq.m
                                       : 0m);

                if (lookedUpRate == 0m)
                {
                    _logger.LogWarning(
                        "[RateLookupApplicator] Found rate entity but rate value is 0. RateId={RateId}",
                        rateEntity.Id);
                    return baseRate;
                }

                // Apply percentage/multiplier
                // effectValue = 50 means 50% of the looked-up rate
                var result = lookedUpRate * (effectValue / 100m);

                _logger.LogInformation(
                    "[RateLookupApplicator] Looked up rate={LookedUpRate} for TypeOfUseGroup={TypeOfUseGroupId}, " +
                    "applied {Percentage}%, result={Result}",
                    lookedUpRate, typeOfUseGroupId, effectValue, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RateLookupApplicator] Error looking up rate. Returning baseRate unchanged.");
                return baseRate;
            }
            finally
            {
                // Clear context after use to prevent stale data
                _lookupContext = null;
                _inputDict = null;
            }
        }

        private static int? GetContextInt(Dictionary<string, object>? context, string key)
        {
            if (context == null || !context.TryGetValue(key, out var value) || value == null)
                return null;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;

            return null;
        }
    }
}
