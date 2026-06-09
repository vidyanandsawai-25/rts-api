using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Responsible for loading, validating, and assembling the complete
    /// <see cref="PropertyCalculationContext"/> from the database.
    ///
    /// <para>
    /// This service is the single entry point for obtaining a ready-to-use context.
    /// Callers (e.g. <c>RateableValueService</c>) should never query the property,
    /// assessment, or detail repositories directly — they should call this service instead.
    /// </para>
    ///
    /// <para>
    /// <b>Responsibilities:</b><br/>
    /// 1. Parallel-fetch property, assessment, social details, and property details.<br/>
    /// 2. Validate required data (property exists, details present, construction year parseable).<br/>
    /// 3. Resolve the applicable assessment year range.<br/>
    /// 4. Sequential-fetch renter and occupancy child collections.<br/>
    /// 5. Assemble and return a fully populated <see cref="PropertyCalculationContext"/>.
    /// </para>
    /// </summary>
    public class PropertyContextLoaderService : IPropertyContextLoaderService
    {
        private readonly IRepository<PropertyEntity, int> _propertyRepo;
        private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepo;
        private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepo;
        private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepo;
        private readonly IRepository<RenterMastEntity, int> _renterRepo;
        private readonly IRepository<PropertyOccupancyDetailsEntity, int> _occupancyRepo;
        private readonly TaxMasterDataService _masterDataService;
        private readonly ILogger<PropertyContextLoaderService> _logger;

        public PropertyContextLoaderService(
            IRepository<PropertyEntity, int> propertyRepo,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
            IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepo,
            IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepo,
            IRepository<RenterMastEntity, int> renterRepo,
            IRepository<PropertyOccupancyDetailsEntity, int> occupancyRepo,
            TaxMasterDataService masterDataService,
            ILogger<PropertyContextLoaderService> logger)
        {
            _propertyRepo               = propertyRepo;
            _propertyDetailsRepo        = propertyDetailsRepo;
            _propertyAssessmentRepo     = propertyAssessmentRepo;
            _propertySocialDetailsRepo  = propertySocialDetailsRepo;
            _renterRepo                 = renterRepo;
            _occupancyRepo              = occupancyRepo;
            _masterDataService          = masterDataService;
            _logger                     = logger;
        }

        /// <inheritdoc/>
        public async Task<PropertyCalculationContext> LoadPropertyContextAsync(
            int propertyId,
            int financeYear,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Loading PropertyCalculationContext for PropertyId={PropertyId}, FinanceYear={FinanceYear}",
                propertyId, financeYear);

            // ── Phase 1: Core Property Fetch ──────────────────────────────────────

            var property = await _propertyRepo.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion,
                    cancellationToken);

            if (property == null)
                throw new InvalidOperationException($"Property not found for PropertyId={propertyId}");

            // ── Phase 2: Parallel Fetch of child details ───────────────────────────

            var assessmentTask = _propertyAssessmentRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // Load ALL social attributes for this property in one query.
            // Each row maps SocialAttributeCode → typed value (bit/int/decimal/text).
            // This means ANY attribute from SocialAttributeMaster is available in rule
            // expressions as  input.HAS_LIFT, input.NO_OF_WELL, input.HAS_SOLAR, etc.
            // with ZERO code changes when new attributes are added to the master table.
            var socialDetailsTask = _propertySocialDetailsRepo.GetQueryable()
                .AsNoTracking()
                .Where(psd => psd.PropertyId == propertyId && psd.IsActive && psd.SocialAttribute != null)
                .Select(psd => new
                {
                    Code       = psd.SocialAttribute!.SocialAttributeCode,
                    DataType   = psd.SocialAttribute!.DataType,
                    BitValue   = psd.BitValue,
                    IntValue   = psd.IntValue,
                    DecimalValue = psd.DecimalValue,
                    TextValue  = psd.TextValue
                })
                .ToListAsync(cancellationToken);

            var detailsTask = _propertyDetailsRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(assessmentTask, socialDetailsTask, detailsTask);

            var assessment    = assessmentTask.Result;
            var socialDetails = socialDetailsTask.Result;
            var details       = detailsTask.Result;

            // Derive hasLift from the loaded social attributes (backward-compat)
            var hasLift = socialDetails.Any(s =>
                s.Code == "HAS_LIFT" &&
                s.BitValue == true);

            // Build a flat attribute dictionary: SocialAttributeCode → typed CLR value
            // Rule expressions can reference these directly: input.HAS_LIFT, input.NO_OF_WELL, etc.
            var socialAttributeDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in socialDetails)
            {
                if (string.IsNullOrWhiteSpace(attr.Code)) continue;
                object? val = attr.DataType?.ToUpperInvariant() switch
                {
                    "BIT"     => (object?)(attr.BitValue ?? false),
                    "INT"     => attr.IntValue,
                    "DECIMAL" => attr.DecimalValue,
                    "TEXT"    => attr.TextValue,
                    _         => attr.BitValue.HasValue ? attr.BitValue
                                 : attr.IntValue.HasValue ? (object?)attr.IntValue
                                 : attr.DecimalValue.HasValue ? attr.DecimalValue
                                 : attr.TextValue
                };
                if (val != null)
                    socialAttributeDict[attr.Code] = val;
            }

            // ── Phase 3: Core validations ──────────────────────────────────────────

            if (assessment == null)
            {
                _logger.LogWarning(
                    "PropertyAssessmentEntity not found for PropertyId={PropertyId}. " +
                    "OwnerType will default to 0 in the rule engine. This may cause incorrect rule matching.",
                    propertyId);
            }

            CalculationValidator.CheckCondition(
                details.Any(),
                $"PropertyDetails not found for PropertyId={propertyId}");

            // ── Phase 3: Parse construction year ───────────────────────────────────

            var constructionYear = details[0].ConstructionYear;

            CalculationValidator.CheckCondition(
                !string.IsNullOrWhiteSpace(constructionYear),
                $"ConstructionYear not found for PropertyId={propertyId}");

            CalculationValidator.CheckCondition(
                int.TryParse(constructionYear, out int constructionYearValue),
                $"Invalid ConstructionYear value '{constructionYear}' for PropertyId={propertyId}");

            // ── Phase 4: Resolve assessment year range ─────────────────────────────

            var yearRanges = await _masterDataService.GetActiveYearRangesAsync();

            var yearRange = yearRanges.FirstOrDefault(
                                x => x.FromYear <= constructionYearValue
                                  && x.ToYear   >= constructionYearValue)
                            ?? throw new InvalidOperationException(
                                $"Assessment year range not found for constructionYear={constructionYearValue}");

            // ── Phase 5: Child collections (sequential — depend on detail IDs) ─────

            var detailIds = details.Select(d => d.Id).ToList();

            var renters = await _renterRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => detailIds.Contains(x.PropertyDetailsId) && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var occupancies = await _occupancyRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => detailIds.Contains(x.PropertyDetailId) && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            // ── Phase 6: Assemble and return the context ───────────────────────────

            return new PropertyCalculationContext
            {
                Property            = property,
                PropertyAssessment  = assessment,
                Details             = details,
                Renters             = renters,
                Occupancies         = occupancies,

                Parameters = new PropertyCalculationParameters
                {
                    FinanceYear              = financeYear,
                    ConstructionYearValue    = constructionYearValue,
                    YearRangeRVId            = yearRange.Id,
                    HasLift                  = hasLift,
                    SocialAttributes         = socialAttributeDict
                    // Detail and DetailTypeOfUse remain null at the root context level.
                    // They are populated per-detail by PropertyCalculationContext.CloneForDetail().
                }
            };
        }
    }
}
