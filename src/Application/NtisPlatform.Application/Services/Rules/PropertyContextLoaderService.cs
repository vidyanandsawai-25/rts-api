using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    /// 4. Sequential-fetch renter and certificate child collections.<br/>
    /// 5. Assemble and return a fully populated <see cref="PropertyCalculationContext"/>.
    /// </para>
    /// </summary>
    public class PropertyContextLoaderService : IPropertyContextLoaderService
    {
        /// <summary>
        /// Sequence number threshold representing the First Floor.
        /// Floors with SequenceNo >= 12 correspond to upper-structure floors (First Floor and above),
        /// while sequence numbers below 12 represent Ground Floor, Basements, etc.
        /// </summary>
        private const int FirstFloorSequenceThreshold = 12;

        private readonly IRepository<PropertyEntity, int> _propertyRepo;
        private readonly IRepository<PropertyCategoryEntity, int> _categoryRepo;
        private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepo;
        private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepo;
        private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepo;
        private readonly IRepository<RenterMastEntity, int> _renterRepo;
        private readonly IRepository<PropertyCertificateEntity, int> _propertyCertificateRepo;
        private readonly ITaxMasterDataService _masterDataService;
        private readonly IFinanceYearProvider _financeYearProvider;
        private readonly IRepository<YearMasterEntity, int> _yearMasterRepo;
        private readonly IRVCalculationCleanupService _rvCalculationCleanupService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PropertyContextLoaderService> _logger;


        public PropertyContextLoaderService(
            IRepository<PropertyEntity, int> propertyRepo,
            IRepository<PropertyCategoryEntity, int> categoryRepo,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
            IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepo,
            IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepo,
            IRepository<RenterMastEntity, int> renterRepo,
            IRepository<PropertyCertificateEntity, int> propertyCertificateRepo,
            ITaxMasterDataService masterDataService,
            IFinanceYearProvider financeYearProvider,
            IRepository<YearMasterEntity, int> yearMasterRepo,
            IRVCalculationCleanupService rvCalculationCleanupService,
            IUnitOfWork unitOfWork,
            ILogger<PropertyContextLoaderService> logger)
        {
            _propertyRepo = propertyRepo;
            _categoryRepo = categoryRepo;
            _propertyDetailsRepo = propertyDetailsRepo;
            _propertyAssessmentRepo = propertyAssessmentRepo;
            _propertySocialDetailsRepo = propertySocialDetailsRepo;
            _renterRepo = renterRepo;
            _propertyCertificateRepo = propertyCertificateRepo;
            _masterDataService = masterDataService;
            _financeYearProvider = financeYearProvider;
            _yearMasterRepo = yearMasterRepo;
            _rvCalculationCleanupService = rvCalculationCleanupService;
            _unitOfWork = unitOfWork;
            _logger = logger;
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

            var propertyWithCategory = await (
                from p in _propertyRepo.GetQueryable().AsNoTracking()
                join c in _categoryRepo.GetQueryable().AsNoTracking() on p.CategoryId equals c.Id into categoryJoin
                from c in categoryJoin.DefaultIfEmpty()
                where p.Id == propertyId && p.IsActive && !p.MarkedForDeletion
                select new 
                { 
                    Property = p, 
                    CategoryName = c != null ? c.PropertyCategoryName : null 
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (propertyWithCategory == null)
                throw new InvalidOperationException($"Property not found for PropertyId={propertyId}");

            var property = propertyWithCategory.Property;
            var categoryName = propertyWithCategory.CategoryName;

            bool isApartmentOrIndustry = categoryName != null && (
                categoryName.Equals("Apartment", StringComparison.OrdinalIgnoreCase) ||
                categoryName.Equals("Industry", StringComparison.OrdinalIgnoreCase));

            // ── Phase 2: Sequential Fetch of child details ───────────────────────────

            var assessment = await _propertyAssessmentRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // Gather all property IDs to fetch social attributes from:
            // 1. Current property (could be partitioned)
            // 2. Main property (same PropertyNo and WardId, but with no partition) - ONLY if category is Apartment or Industry
            var targetPropertyIds = new List<int> { propertyId };
            if (isApartmentOrIndustry && !string.IsNullOrWhiteSpace(property.PropertyNo))
            {
                var mainPropertyId = await _propertyRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.WardId == property.WardId 
                             && p.PropertyNo == property.PropertyNo 
                             && (p.PartitionNo == null || p.PartitionNo == "")
                             && p.IsActive 
                             && !p.MarkedForDeletion)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (mainPropertyId.HasValue && mainPropertyId.Value != propertyId)
                {
                    targetPropertyIds.Add(mainPropertyId.Value);
                }
            }

            // Load ALL social attributes for the target property and main property in one query.
            // Each row maps SocialAttributeCode → typed value (bit/int/decimal/text).
            // This means ANY attribute from SocialAttributeMaster is available in rule
            // expressions as  input.HAS_LIFT, input.NO_OF_WELL, input.HAS_SOLAR, etc.
            // with ZERO code changes when new attributes are added to the master table.
            var socialDetails = await _propertySocialDetailsRepo.GetQueryable()
                .AsNoTracking()
                .Where(psd => targetPropertyIds.Contains(psd.PropertyId) && psd.IsActive && psd.SocialAttribute != null)
                .Select(psd => new
                {
                    PropertyId = psd.PropertyId,
                    SocialAttributeId = psd.SocialAttributeId,
                    Code = psd.SocialAttribute!.SocialAttributeCode,
                    DataType = psd.SocialAttribute!.DataType,
                    BitValue = psd.BitValue,
                    IntValue = psd.IntValue,
                    DecimalValue = psd.DecimalValue,
                    TextValue = psd.TextValue
                })
                .ToListAsync(cancellationToken);

            var details = await _propertyDetailsRepo.GetQueryable()
                .Include(x => x.Floor)
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            if (!details.Any())
            {
                int financeYearForCleanup = financeYear > 0
                    ? financeYear
                    : _financeYearProvider.GetCurrentFinanceYear();

                var yearMasterForCleanup = await _yearMasterRepo.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        y => y.Year == financeYearForCleanup && y.IsActive,
                        cancellationToken);

                await _rvCalculationCleanupService.DeactivateExistingRVCalculationsAsync(
                    propertyId,
                    financeYearForCleanup,
                    yearMasterForCleanup?.Id);

                await _unitOfWork.SaveChangesAsync();

                var emptyYearRanges = await _masterDataService.GetActiveYearRangesAsync();

                return new PropertyCalculationContext
                {
                    Property = property,
                    Details = new List<PropertyDetailsEntity>(),
                    YearRanges = emptyYearRanges,
                    Parameters = new PropertyCalculationParameters { FinanceYear = financeYearForCleanup }
                };
            }

            // Gather active SocialAttributeIds for the property (distinct list)
            var socialAttributeIds = socialDetails.Select(s => s.SocialAttributeId).Distinct().ToList();

            // Build a flat attribute dictionary: SocialAttributeCode → typed CLR value
            // Rule expressions can reference these directly: input.HAS_LIFT, input.NO_OF_WELL, etc.
            var socialAttributeDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            // Sort by PropertyId such that main property comes first, and current property comes last.
            // This ensures current property's attributes override main property's in case of duplicates.
            var orderedSocialDetails = socialDetails.OrderBy(s => s.PropertyId == propertyId ? 1 : 0);
            
            foreach (var attr in orderedSocialDetails)
            {
                if (string.IsNullOrWhiteSpace(attr.Code)) continue;
                object? val = attr.DataType?.ToUpperInvariant() switch
                {
                    "BIT" => (object?)(attr.BitValue ?? false),
                    "INT" => attr.IntValue,
                    "DECIMAL" => attr.DecimalValue,
                    "TEXT" => attr.TextValue,
                    _ => attr.BitValue.HasValue ? attr.BitValue
                                 : attr.IntValue.HasValue ? (object?)attr.IntValue
                                 : attr.DecimalValue.HasValue ? attr.DecimalValue
                                 : attr.TextValue
                };
                if (val != null)
                    socialAttributeDict[attr.Code] = val;
            }

            // ── Phase 3: Parse construction year ───────────────────────────────────

            var constructionYear = details[0].ConstructionYear;

            CalculationValidator.CheckCondition(
                !string.IsNullOrWhiteSpace(constructionYear),
                $"ConstructionYear not found for PropertyId={propertyId}");

            CalculationValidator.CheckCondition(
                int.TryParse(constructionYear, out int constructionYearValue),
                $"Invalid ConstructionYear value '{constructionYear}' for PropertyId={propertyId}");

            // ── Phase 4: Resolve assessment year ranges ────────────────────────────
            // Note: Assessment year is now resolved per-detail during CloneForDetail().
            // This loads all active year ranges for later use when calculating YearRangeRVIdForDetail.

            var yearRanges = await _masterDataService.GetActiveYearRangesAsync();

            // Fallback to construction year range if no details have assessment years.
            // This maintains backward compatibility.
            var yearRange = yearRanges.FirstOrDefault(
                                x => x.FromYear <= constructionYearValue
                                  && x.ToYear >= constructionYearValue)
                            ?? throw new InvalidOperationException(
                                $"Assessment year range not found for constructionYear={constructionYearValue}");

            // ── Phase 5: Child collections (sequential — depend on detail IDs) ─────

            var detailIds = details.Select(d => d.Id).ToList();

            var renters = await _renterRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => detailIds.Contains(x.PropertyDetailsId) && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var certificates = (await _propertyCertificateRepo.GetQueryable()
                .Include(pc => pc.CertificateType)
                .AsNoTracking()
                .Where(pc => pc.PropertyDetailsId.HasValue
                          && detailIds.Contains(pc.PropertyDetailsId.Value)
                          && pc.IsActive && !pc.MarkedForDeletion
                          && pc.CertificateType != null)
                .ToListAsync(cancellationToken))
                .Where(pc => string.Equals(pc.CertificateType!.CertificateTypeCode, CertificateTypeCodes.OC, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // ── Phase 6: Assemble and return the context ───────────────────────────

            // Calculate building's max floor sequence number across all related properties in the same building (single optimized query)
            // Only search related properties in the building if category is Apartment or Industry.
            // Otherwise, go with the provided propertyId.
            IQueryable<int> propertyIdsQuery;
            if (isApartmentOrIndustry && !string.IsNullOrWhiteSpace(property.PropertyNo))
            {
                propertyIdsQuery = _propertyRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.WardId == property.WardId 
                             && p.PropertyNo == property.PropertyNo 
                             && p.IsActive 
                             && !p.MarkedForDeletion)
                    .Select(p => p.Id);
            }
            else
            {
                propertyIdsQuery = _propertyRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.Id == propertyId)
                    .Select(p => p.Id);
            }

            var propertyIds = await propertyIdsQuery.ToListAsync(cancellationToken);

            var maxFloorSequenceNullable = await _propertyDetailsRepo.GetQueryable()
                .AsNoTracking()
                .Where(d => propertyIds.Contains(d.PropertyId)
                          && d.IsActive 
                          && !d.MarkedForDeletion
                          && d.Floor != null 
                          && d.Floor.IsActive 
                          && d.Floor.SequenceNo >= FirstFloorSequenceThreshold)
                .Select(d => (int?)d.Floor!.SequenceNo)
                .MaxAsync(cancellationToken);

            var maxFloorSequence = maxFloorSequenceNullable ?? 0;

            // Pre-compute YearRangeRVId for each detail based on AssessmentYear
            var detailYearRangeRVIdMap = new Dictionary<int, int>();
            foreach (var detail in details)
            {
                var detailYearRangeRVId = ResolveYearRangeRVIdForDetail(detail, yearRanges, yearRange.Id);
                detailYearRangeRVIdMap[detail.Id] = detailYearRangeRVId;
            }

            return new PropertyCalculationContext
            {
                Property = property,
                PropertyAssessment = assessment,
                Details = details,
                Renters = renters,
                Certificates = certificates,
                YearRanges = yearRanges,
                DetailYearRangeRVIdMap = detailYearRangeRVIdMap,

                Parameters = new PropertyCalculationParameters
                {
                    FinanceYear = financeYear,
                    ConstructionYearValue = constructionYearValue,
                    YearRangeRVId = yearRange.Id,
                    SocialAttributeId = socialAttributeIds,
                    SocialAttributes = socialAttributeDict,
                    BuildingMaxFloorSequence = maxFloorSequence
                    // Detail and DetailTypeOfUse remain null at the root context level.
                    // They are populated per-detail by PropertyCalculationContext.CloneForDetail().
                }
            };
        }

        /// <summary>
        /// Resolves the YearRangeRVId for a specific detail based on its AssessmentYear.
        /// 1. If detail has AssessmentYear: finds matching year range where FromYear ≤ AssessmentYear ≤ ToYear
        ///    If not found: returns 0 (NO FALLBACK - will apply 0 tax)
        /// 2. If no AssessmentYear: falls back to detail's ConstructionYear
        ///    If still not found: uses property-level year range
        /// </summary>
        private int ResolveYearRangeRVIdForDetail(
            PropertyDetailsEntity detail,
            List<AssessmentYearRangeEntity> yearRanges,
            int propertyLevelYearRangeRVId)
        {
            // Primary: Try to resolve from detail's assessment year
            if (!string.IsNullOrWhiteSpace(detail.AssessmentYear) &&
                int.TryParse(detail.AssessmentYear, out int assessmentYear))
            {
                var matchingYearRange = yearRanges.FirstOrDefault(y =>
                    y.FromYear <= assessmentYear && y.ToYear >= assessmentYear && y.IsActive);

                if (matchingYearRange != null)
                    return matchingYearRange.Id;

                // ❌ AssessmentYear provided but NOT FOUND in any year range
                // NO FALLBACK - Return 0 to indicate 0 tax should be applied
                _logger.LogWarning(
                    "AssessmentYear={AssessmentYear} not found in any AssessmentYearRangeEntity for DetailId={DetailId}. " +
                    "Zero tax will be applied.",
                    detail.AssessmentYear, detail.Id);
                return 0;  // Signal: 0 tax
            }

            // Fallback: AssessmentYear is NULL/empty - try detail's ConstructionYear
            if (!string.IsNullOrWhiteSpace(detail.ConstructionYear) &&
                int.TryParse(detail.ConstructionYear, out int constructionYear))
            {
                var matchingYearRange = yearRanges.FirstOrDefault(y =>
                    y.FromYear <= constructionYear && y.ToYear >= constructionYear && y.IsActive);

                if (matchingYearRange != null)
                    return matchingYearRange.Id;
            }

            // Final fallback: Use property-level year range (last resort)
            return propertyLevelYearRangeRVId;
        }
    }
}
