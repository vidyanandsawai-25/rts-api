using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service implementation for tax applicability operations
/// </summary>
public class TaxApplicabilityService : BaseCommonCrudService<ApplyTaxesMasterEntity, TaxApplicabilityResponseDto, CreateTaxApplicabilityRequestDto, UpdateTaxApplicabilityRequestDto, TaxApplicabilityRequestDto, int>, ITaxApplicabilityService
{
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<TaxPercentageMasterRVEntity, int> _taxPercentageRVRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<AssessmentYearRangeEntity, int> _yearRangeRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IRepository<RVCalculationResultsEntity, int> _rvCalculationResultsRepository;
    private readonly IRepository<RVCalculationTaxDetailsEntity, int> _rvCalculationTaxDetailsRepository;
    private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepository;
    private readonly IRepository<TaxConditionRuleEntity, int> _taxConditionRuleRepository;
    private readonly IRepository<TaxMasterMappingEntity, int> _taxMasterMappingRepository;
    private readonly IRepository<EducationTaxMasterEntity, int> _educationTaxMasterRepository;
    private readonly IRepository<EmploymentTaxMasterEntity, int> _employmentTaxMasterRepository;

    public TaxApplicabilityService(
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<TaxPercentageMasterRVEntity, int> taxPercentageRVRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<AssessmentYearRangeEntity, int> yearRangeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IRepository<RVCalculationResultsEntity, int> rvCalculationResultsRepository,
        IRepository<RVCalculationTaxDetailsEntity, int> rvCalculationTaxDetailsRepository,
        IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepository,
        IRepository<TaxConditionRuleEntity, int> taxConditionRuleRepository,
        IRepository<TaxMasterMappingEntity, int> taxMasterMappingRepository,
        IRepository<EducationTaxMasterEntity, int> educationTaxMasterRepository,
        IRepository<EmploymentTaxMasterEntity, int> employmentTaxMasterRepository,
        IRepository<ApplyTaxesMasterEntity, int> applyTaxesRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(applyTaxesRepository, unitOfWork, mapper)
    {
        _taxMasterRepository = taxMasterRepository;
        _taxPercentageRVRepository = taxPercentageRVRepository;
        _transMastRepository = transMastRepository;
        _yearRangeRepository = yearRangeRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _yearMasterRepository = yearMasterRepository;
        _rvCalculationResultsRepository = rvCalculationResultsRepository;
        _rvCalculationTaxDetailsRepository = rvCalculationTaxDetailsRepository;
        _propertySocialDetailsRepository = propertySocialDetailsRepository;
        _taxConditionRuleRepository = taxConditionRuleRepository;
        _taxMasterMappingRepository = taxMasterMappingRepository;
        _educationTaxMasterRepository = educationTaxMasterRepository;
        _employmentTaxMasterRepository = employmentTaxMasterRepository;
    }

    public override async Task<PagedResult<TaxApplicabilityResponseDto>> GetAllAsync(
        TaxApplicabilityRequestDto queryParameters,
        CancellationToken cancellationToken = default)
    {
        var result = await GetTaxApplicabilityAsync(queryParameters, cancellationToken);
        return new PagedResult<TaxApplicabilityResponseDto>(new List<TaxApplicabilityResponseDto> { result }, 1, 1, 1);
    }

    public override async Task<TaxApplicabilityResponseDto> CreateAsync(
        CreateTaxApplicabilityRequestDto createDto,
        CancellationToken cancellationToken = default)
    {
        var message = await CreateTaxApplicabilityAsync(createDto, cancellationToken);
        return new TaxApplicabilityResponseDto
        {
            PropertyId = createDto.PropertyId,
            ApplicableTaxes = createDto.Taxes.Select(t => new TaxApplicabilityDetailDto
            {
                TaxId = t.TaxId,
                IsApplicable = t.IsApplicable,
                IsActive = t.IsApplicable
            }).ToList()
        };
    }

    public override async Task<TaxApplicabilityResponseDto?> UpdateAsync(
        int id,
        UpdateTaxApplicabilityRequestDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var message = await UpdateTaxApplicabilityAsync(id, updateDto, cancellationToken);
        return new TaxApplicabilityResponseDto
        {
            PropertyId = updateDto.PropertyId,
            ApplicableTaxes = updateDto.Taxes.Select(t => new TaxApplicabilityDetailDto
            {
                TaxId = t.TaxId,
                IsApplicable = t.IsApplicable,
                IsActive = t.IsApplicable
            }).ToList()
        };
    }

    public async Task<TaxApplicabilityResponseDto> GetTaxApplicabilityAsync(
        TaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = new TaxApplicabilityResponseDto
        {
            PropertyId = request.PropertyId,
            AssessmentYearRangeId = request.AssessmentYearRangeId,
            TypeOfUseId = request.TypeOfUseId
        };

        // 1. Resolve YearRangeRVId using AssessmentYearRange (and fallback to YearMaster if needed)
        int? yearRangeRVId = null;

        // Try direct lookup in AssessmentYearRangeMasterRV (_yearRangeRepository)
        var assessmentYearRange = await _yearRangeRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(yr => yr.Id == request.AssessmentYearRangeId && yr.IsActive, cancellationToken);

        if (assessmentYearRange != null)
        {
            yearRangeRVId = assessmentYearRange.Id;
        }
        else
        {
            // Fallback: lookup via YearMaster if AssessmentYearRangeId represents a single YearMaster Id
            var yearMaster = await _yearMasterRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(ym => ym.Id == request.AssessmentYearRangeId, cancellationToken);

            if (yearMaster != null)
            {
                var yearRange = await _yearRangeRepository.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(yr => yr.FromYear <= yearMaster.Year && yr.ToYear >= yearMaster.Year && yr.IsActive, cancellationToken);
                if (yearRange != null)
                {
                    yearRangeRVId = yearRange.Id;
                }
            }
        }

        var calcTypeNormalized = (request.CalculationType ?? string.Empty).Trim().ToUpperInvariant();

        // 2. Query TaxMaster joined with TaxPercentageMasterRV (INNER JOIN), TransMast (LEFT JOIN), and ApplyTaxesMaster (LEFT JOIN) according to exact SQL criteria
        var query = from tm in _taxMasterRepository.GetQueryable()
                        .Where(x => x.AssessmentStatus)

                    join tpr in _taxPercentageRVRepository.GetQueryable()
                        .Where(x => (yearRangeRVId == null || x.YearRangeRVId == yearRangeRVId)
                                 && x.TypeOfUseId == request.TypeOfUseId 
                                 && x.IsActive)
                        on tm.Id equals tpr.TaxId

                    join tr in _transMastRepository.GetQueryable()
                        .Where(x => x.PropertyId == request.PropertyId
                                 && x.CalculationType.Trim().ToUpper() == calcTypeNormalized
                                 && !x.MarkedForDeletion)
                        on tm.Id equals tr.TaxId into trGroup
                    from tr in trGroup.DefaultIfEmpty()

                    join app in _repository.GetQueryable()
                        .Where(x => x.PropertyId == request.PropertyId
                                 && x.IsActive
                                 && !x.MarkedForDeletion)
                        on tm.Id equals app.TaxId into appGroup
                    from app in appGroup.DefaultIfEmpty()

                    group new { tm, tpr, tr, app } by new
                    {
                        tm.Id,
                        tm.TaxName,
                        tm.TaxCode,
                        tm.DisplayOrder,
                        tm.IsActive,
                        tm.AssessmentStatus,
                        trCalculationType = tr != null ? tr.CalculationType : null
                    } into g
                    orderby g.Key.DisplayOrder
                    select new TaxApplicabilityDetailDto
                    {
                        TaxId = g.Key.Id,
                        TaxHead = g.Key.TaxName,
                        TaxCode = g.Key.TaxCode ?? string.Empty,
                        CalculationType = g.Key.trCalculationType,
                        TaxPercentage = g.Max(x => (decimal?)x.tpr.TaxPercentage) ?? 0,
                        TaxAmount = g.Max(x => x.tr != null ? (decimal?)x.tr.TaxAmount : null) ?? 0,
                        // CASE WHEN COUNT(tpr.Id) > 0 AND COUNT(app.Id) = 0 THEN 1 ELSE 0 END
                        IsApplicable = g.Any(x => x.tpr != null) && !g.Any(x => x.app != null),
                        IsActive = g.Key.IsActive,
                        AssessmentStatus = g.Key.AssessmentStatus
                    };

        var taxDetails = await query.ToListAsync(cancellationToken);

        foreach (var taxDetail in taxDetails)
        {
            if (taxDetail.IsApplicable)
            {
                response.ApplicableTaxes.Add(taxDetail);
            }
            else
            {
                response.ExemptedTaxes.Add(taxDetail);
            }
        }

        response.ApplicableCount = response.ApplicableTaxes.Count;
        response.ExemptedCount = response.ExemptedTaxes.Count;

        // Populate TaxCalculations array
        var calculationResponse = await GetTaxApplicabilityCalculationAsync(request.PropertyId, null, cancellationToken);
        response.TaxCalculations = calculationResponse.TaxCalculations;

        // Calculate and populate Summary card header metrics
        var propertyDetails = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.PropertyId == request.PropertyId && !pd.MarkedForDeletion)
            .Include(pd => pd.TypeOfUse)
            .ToListAsync(cancellationToken);

        var propertyDetailsIds = propertyDetails.Select(pd => pd.Id).ToList();

        var rvResults = await _rvCalculationResultsRepository.GetQueryable()
            .AsNoTracking()
            .Where(rcr => propertyDetailsIds.Contains(rcr.PropertyDetailsId) && !rcr.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        decimal resRv = 0m;
        decimal commRv = 0m;

        foreach (var pd in propertyDetails)
        {
            var rvs = rvResults.Where(rcr => rcr.PropertyDetailsId == pd.Id).Select(rcr => rcr.RateableValue ?? 0m);
            var pdRv = rvs.Any() ? rvs.Max() : 0m;

            var typeStr = (pd.TypeOfUse?.Type ?? pd.TypeOfUse?.Description ?? "").Trim().ToUpperInvariant();
            if (typeStr.StartsWith("R") || typeStr.Contains("RESIDENT"))
            {
                resRv += pdRv;
            }
            else
            {
                commRv += pdRv;
            }
        }

        double totalArea = propertyDetails.Sum(pd => pd.BuiltupAreaSqMeter ?? pd.CarpetAreaSqMeter ?? 0);
        int totalToilets = propertyDetails.Sum(pd => pd.NoOfRooms ?? 0);
        decimal totalTaxAmount = response.ApplicableTaxes.Sum(t => t.TaxAmount);

        response.Summary = new TaxApplicabilityHeaderSummaryDto
        {
            TotalTax = totalTaxAmount,
            ResidentialRV = resRv,
            CommercialRV = commRv,
            Area = totalArea,
            Toilets = totalToilets
        };

        return response;
    }

    public async Task<string> CreateTaxApplicabilityAsync(
        CreateTaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch existing exemption records from ApplyTaxesMaster for this PropertyId (including marked for deletion or inactive)
        var existingExemptions = await _repository
            .GetQueryable()
            .Where(x => x.PropertyId == request.PropertyId)
            .ToListAsync(cancellationToken);

        // 2. Fetch tax master details for the requested taxes
        var requestedTaxIds = request.Taxes.Select(t => t.TaxId).Distinct().ToList();
        var taxMasters = await _taxMasterRepository
            .GetQueryable()
            .Where(t => requestedTaxIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        // Check for missing taxes
        var foundTaxIds = taxMasters.Select(t => t.Id).ToList();
        var missingTaxIds = requestedTaxIds.Except(foundTaxIds).ToList();
        if (missingTaxIds.Any())
        {
            throw new ArgumentException($"Cannot create tax applicability. The following Tax ID(s) do not exist: {string.Join(", ", missingTaxIds)}.");
        }

        // Check for inactive taxes
        var inactiveTaxes = taxMasters.Where(t => !t.IsActive).ToList();
        if (inactiveTaxes.Any())
        {
            var inactiveTaxNames = string.Join(", ", inactiveTaxes.Select(t => $"{t.TaxName} ({t.TaxCode})"));
            throw new ArgumentException($"Cannot create tax applicability. The following tax(es) are inactive in Tax Master: {inactiveTaxNames}.");
        }

        // 3. Check for duplicate entries - same isApplicable status
        var duplicateTaxes = new List<string>();
        foreach (var taxStatus in request.Taxes)
        {
            var existingRecord = existingExemptions.FirstOrDefault(x => x.TaxId == taxStatus.TaxId);
            if (existingRecord != null)
            {
                bool currentIsApplicable = !existingRecord.IsActive;
                if (currentIsApplicable == taxStatus.IsApplicable)
                {
                    var taxMaster = taxMasters.FirstOrDefault(t => t.Id == taxStatus.TaxId);
                    var statusText = taxStatus.IsApplicable ? "applicable" : "exempted";
                    duplicateTaxes.Add($"{taxMaster?.TaxName ?? $"Tax ID {taxStatus.TaxId}"} (already {statusText})");
                }
            }
        }

        if (duplicateTaxes.Any())
        {
            throw new InvalidOperationException(
                $"Cannot create tax applicability. The following tax(es) already have the same status: {string.Join(", ", duplicateTaxes)}. " +
                $"No changes are needed for these taxes.");
        }

        bool anyChange = false;

        // 4. Loop through all incoming tax statuses
        foreach (var taxStatus in request.Taxes)
        {
            var existingRecord = existingExemptions.FirstOrDefault(x => x.TaxId == taxStatus.TaxId);

            if (existingRecord == null)
            {
                var newRecord = _mapper.Map<ApplyTaxesMasterEntity>(taxStatus);
                newRecord.PropertyId = request.PropertyId;
                newRecord.CreatedBy = request.UserId;
                newRecord.CreatedDate = DateTime.Now;
                
                await _repository.AddAsync(newRecord, cancellationToken);
                anyChange = true;
            }
            else
            {
                bool desiredIsActive = !taxStatus.IsApplicable;
                bool desiredMarkedForDeletion = taxStatus.IsApplicable;

                if (existingRecord.IsActive != desiredIsActive || existingRecord.MarkedForDeletion != desiredMarkedForDeletion)
                {
                    _mapper.Map(taxStatus, existingRecord);
                    existingRecord.UpdatedBy = request.UserId;
                    existingRecord.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(existingRecord, cancellationToken);
                    anyChange = true;
                }
            }
        }

        if (anyChange)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return "Tax applicability created successfully.";
        }

        return "No changes detected. All taxes already have the requested status.";
    }

    public async Task<HashSet<int>> GetExemptedTaxIdsAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        var exemptedTaxIds = await _repository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .Select(x => x.TaxId)
            .ToListAsync(cancellationToken);

        return new HashSet<int>(exemptedTaxIds);
    }

    public async Task<string> UpdateTaxApplicabilityAsync(
        int id,
        UpdateTaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch existing exemption records from ApplyTaxesMaster for this PropertyId
        var existingExemptions = await _repository
            .GetQueryable()
            .Where(x => x.PropertyId == request.PropertyId)
            .ToListAsync(cancellationToken);

        // 2. Fetch tax master details for the requested taxes
        var requestedTaxIds = request.Taxes.Select(t => t.TaxId).Distinct().ToList();
        var taxMasters = await _taxMasterRepository
            .GetQueryable()
            .Where(t => requestedTaxIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        // Check for missing taxes
        var foundTaxIds = taxMasters.Select(t => t.Id).ToList();
        var missingTaxIds = requestedTaxIds.Except(foundTaxIds).ToList();
        if (missingTaxIds.Any())
        {
            throw new ArgumentException($"Cannot update tax applicability. The following Tax ID(s) do not exist: {string.Join(", ", missingTaxIds)}.");
        }

        // Check for inactive taxes
        var inactiveTaxes = taxMasters.Where(t => !t.IsActive).ToList();
        if (inactiveTaxes.Any())
        {
            var inactiveTaxNames = string.Join(", ", inactiveTaxes.Select(t => $"{t.TaxName} ({t.TaxCode})"));
            throw new ArgumentException($"Cannot update tax applicability. The following tax(es) are inactive in Tax Master: {inactiveTaxNames}.");
        }

        // 3. Check for duplicate entries - same isApplicable status
        var duplicateTaxes = new List<string>();
        foreach (var taxStatus in request.Taxes)
        {
            var existingRecord = existingExemptions.FirstOrDefault(x => x.TaxId == taxStatus.TaxId);
            if (existingRecord != null)
            {
                bool currentIsApplicable = !existingRecord.IsActive;
                if (currentIsApplicable == taxStatus.IsApplicable)
                {
                    var taxMaster = taxMasters.FirstOrDefault(t => t.Id == taxStatus.TaxId);
                    var statusText = taxStatus.IsApplicable ? "applicable" : "exempted";
                    duplicateTaxes.Add($"{taxMaster?.TaxName ?? $"Tax ID {taxStatus.TaxId}"} (already {statusText})");
                }
            }
        }

        if (duplicateTaxes.Any())
        {
            throw new InvalidOperationException(
                $"Cannot update tax applicability. The following tax(es) already have the same status: {string.Join(", ", duplicateTaxes)}. " +
                $"No changes are needed for these taxes.");
        }

        bool anyChange = false;

        // 4. Loop through all incoming tax statuses
        foreach (var taxStatus in request.Taxes)
        {
            var existingRecord = existingExemptions.FirstOrDefault(x => x.TaxId == taxStatus.TaxId);

            if (existingRecord == null)
            {
                // For updates, we still allow creating new records if they don't exist
                var newRecord = _mapper.Map<ApplyTaxesMasterEntity>(taxStatus);
                newRecord.PropertyId = request.PropertyId;
                newRecord.CreatedBy = request.UserId;
                newRecord.CreatedDate = DateTime.Now;
                
                await _repository.AddAsync(newRecord, cancellationToken);
                anyChange = true;
            }
            else
            {
                bool desiredIsActive = !taxStatus.IsApplicable;
                bool desiredMarkedForDeletion = taxStatus.IsApplicable;

                if (existingRecord.IsActive != desiredIsActive || existingRecord.MarkedForDeletion != desiredMarkedForDeletion)
                {
                    _mapper.Map(taxStatus, existingRecord);
                    existingRecord.UpdatedBy = request.UserId;
                    existingRecord.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(existingRecord, cancellationToken);
                    anyChange = true;
                }
            }
        }

        if (anyChange)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return "Tax applicability updated successfully.";
        }

        return "No changes detected. All taxes already have the requested status.";
    }

    public async Task<IEnumerable<PropertyFinanceYearTypeOfUseDto>> GetPropertyFinanceYearTypeOfUseAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        var uniqueFinanceYears = await _transMastRepository.GetQueryable()
            .AsNoTracking()
            .Where(t => t.PropertyId == propertyId && !t.MarkedForDeletion)
            .Select(t => t.FinanceYearId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var yearMasterEntries = await _yearMasterRepository.GetQueryable()
            .AsNoTracking()
            .Where(ym => uniqueFinanceYears.Contains(ym.Id))
            .ToDictionaryAsync(ym => ym.Id, ym => ym.YearCode ?? ym.Year.ToString(), cancellationToken);

        var propertyDetails = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.PropertyId == propertyId && !pd.MarkedForDeletion)
            .Include(pd => pd.TypeOfUse)
            .ToListAsync(cancellationToken);

        if (propertyDetails == null || !propertyDetails.Any())
        {
            return Enumerable.Empty<PropertyFinanceYearTypeOfUseDto>();
        }

        var result = propertyDetails
            .SelectMany(
                pd => uniqueFinanceYears.DefaultIfEmpty(),
                (pd, fyId) => new PropertyFinanceYearTypeOfUseDto
                {
                    PropertyId = pd.PropertyId,
                    PropertyDetailId = pd.Id,
                    FinanceYearId = fyId != 0 ? fyId : null,
                    FinanceYear = fyId != 0 && yearMasterEntries.TryGetValue(fyId, out var yearStr) ? yearStr : null,
                    FloorId = pd.FloorId,
                    SubFloorId = pd.SubFloorId,
                    TypeOfUseId = pd.TypeOfUseId,
                    TypeOfUseCode = pd.TypeOfUse?.TypeOfUseCode,
                    TypeOfUseDescription = pd.TypeOfUse?.Description
                }
            )
            .GroupBy(x => x.TypeOfUseId)
            .Select(g => g.First())
            .OrderBy(x => x.PropertyDetailId)
            .ThenBy(x => x.FinanceYearId)
            .ToList();

        return result;
    }

    public async Task<TaxApplicabilityCalculationResponseDto> GetTaxApplicabilityCalculationAsync(
        int propertyId,
        int? assessmentYearRangeId = null,
        CancellationToken cancellationToken = default)
    {
        var response = new TaxApplicabilityCalculationResponseDto
        {
            PropertyId = propertyId,
            AssessmentYearRangeId = assessmentYearRangeId ?? 0
        };

        // 1. PropertyData
        var propertyDataQuery = from pd in _propertyDetailsRepository.GetQueryable()
                                join tum in _typeOfUseRepository.GetQueryable().Where(t => t.IsActive)
                                    on pd.TypeOfUseId equals tum.Id
                                where pd.PropertyId == propertyId && !pd.MarkedForDeletion
                                select new
                                {
                                    PropertyDetailsId = pd.Id,
                                    pd.PropertyId,
                                    pd.TypeOfUseId,
                                    tum.Description,
                                    tum.Type
                                };

        var propertyData = await propertyDataQuery.ToListAsync(cancellationToken);

        if (!propertyData.Any())
        {
            return response;
        }

        var propertyDetailsIds = propertyData.Select(pd => pd.PropertyDetailsId).Distinct().ToList();
        var typeOfUseIdsList = propertyData.Select(pd => pd.TypeOfUseId).Distinct().ToList();

        response.TypeOfUseId = typeOfUseIdsList.FirstOrDefault();

        // 2. RVData
        var rvData = await _rvCalculationResultsRepository.GetQueryable()
            .Where(rcr => propertyDetailsIds.Contains(rcr.PropertyDetailsId) && !rcr.MarkedForDeletion)
            .GroupBy(rcr => rcr.PropertyDetailsId)
            .Select(g => new
            {
                PropertyDetailsId = g.Key,
                RateableValue = g.Max(rcr => rcr.RateableValue ?? 0m)
            })
            .ToDictionaryAsync(x => x.PropertyDetailsId, x => x.RateableValue, cancellationToken);

        // 3. TaxTransaction & Exemptions Check
        var exemptedTaxIdSet = await GetExemptedTaxIdsAsync(propertyId, cancellationToken);

        var transMastQuery = _transMastRepository.GetQueryable()
            .Where(tmt => tmt.PropertyId == propertyId && tmt.CalculationType == "RV" && !tmt.MarkedForDeletion);

        var taxTransactions = await transMastQuery
            .GroupBy(tmt => tmt.TaxId)
            .Select(g => new
            {
                TaxId = g.Key,
                TaxAmount = g.Max(tmt => tmt.TaxAmount)
            })
            .ToDictionaryAsync(x => x.TaxId, x => x.TaxAmount, cancellationToken);

        // 4. TaxMasterData
        var taxMasterQuery = from tm in _taxMasterRepository.GetQueryable()
                             where tm.IsActive
                             select new
                             {
                                 TaxId = tm.Id,
                                 tm.TaxName,
                                 tm.CalculationModeId,
                                 tm.RuleDefinitionId,
                                 tm.AssessmentStatus,
                                 tm.IsActive
                             };

        var taxMasterData = await taxMasterQuery.ToListAsync(cancellationToken);
        var taxMasterMap = taxMasterData.ToDictionary(x => x.TaxId);

        // 5. TaxPercentage
        var taxPercentageQuery = from tpm in _taxPercentageRVRepository.GetQueryable()
                                 where tpm.IsActive && tpm.BaseType != null && tpm.BaseType.Trim() != ""
                                       && (!assessmentYearRangeId.HasValue || assessmentYearRangeId.Value == 0 || tpm.YearRangeRVId == assessmentYearRangeId.Value)
                                 group tpm by new
                                 {
                                     tpm.TaxId,
                                     tpm.TypeOfUseId,
                                     BaseType = tpm.BaseType.Trim()
                                 } into g
                                 select new
                                 {
                                     g.Key.TaxId,
                                     g.Key.TypeOfUseId,
                                     g.Key.BaseType,
                                     TaxPercentage = g.Max(x => x.TaxPercentage)
                                 };

        var taxPercentages = await taxPercentageQuery.ToListAsync(cancellationToken);

        // 6. ValueBasedRaw (CalculationModeId = 1, TaxId NOT IN (2, 3))
        var valueBasedRaw = (from tmd in taxMasterData
                             where tmd.CalculationModeId == 1 && tmd.TaxId != 2 && tmd.TaxId != 3
                             join tp in taxPercentages on tmd.TaxId equals tp.TaxId
                             join pd in propertyData on tp.TypeOfUseId equals pd.TypeOfUseId
                             select new
                             {
                                 tmd.TaxId,
                                 tmd.TaxName,
                                 tmd.CalculationModeId,
                                 tmd.RuleDefinitionId,
                                 pd.TypeOfUseId,
                                 pd.Description,
                                 pd.Type,
                                 tp.BaseType,
                                 RateableValue = rvData.TryGetValue(pd.PropertyDetailsId, out var rv) ? rv : 0m,
                                 tp.TaxPercentage,
                                 TaxAmount = taxTransactions.TryGetValue(tmd.TaxId, out var amt) ? (decimal?)amt : null
                             }).ToList();

        var valueBasedTaxList = valueBasedRaw
            .GroupBy(v => v.TaxId)
            .Select(g =>
            {
                var taxId = g.Key;
                var first = g.First();

                var typeOfUseIds = string.Join(", ", g.Select(x => x.TypeOfUseId.ToString()).Distinct());
                var descriptions = string.Join(", ", g.Select(x => x.Description).Where(d => !string.IsNullOrEmpty(d)).Distinct());
                var types = string.Join(", ", g.Select(x => x.Type).Where(t => !string.IsNullOrEmpty(t)).Distinct());
                var baseTypes = string.Join(", ", g.Select(x => x.BaseType).Where(b => !string.IsNullOrEmpty(b)).Distinct());

                return new TaxApplicabilityCalculationDto
                {
                    TaxId = taxId,
                    TaxName = first.TaxName,
                    CalculationModeId = first.CalculationModeId,
                    CalculationMode = "VALUE_BASED",
                    RuleDefinitionId = first.RuleDefinitionId,
                    TypeOfUseIds = typeOfUseIds,
                    Descriptions = descriptions,
                    Types = types,
                    BaseTypes = baseTypes,
                    AverageTaxPercentage = g.Average(x => x.TaxPercentage),
                    TaxAmount = g.Max(x => x.TaxAmount) ?? 0m
                };
            }).ToList();

        // 7. Special Tax (TaxId = 2: EducationTaxData, TaxId = 3: EmploymentTaxData)
        var defaultTypeOfUseIds = string.Join(", ", propertyData.Select(x => x.TypeOfUseId.ToString()).Distinct());
        var defaultDescriptions = string.Join(", ", propertyData.Select(x => x.Description).Where(d => !string.IsNullOrEmpty(d)).Distinct());
        var defaultTypes = string.Join(", ", propertyData.Select(x => x.Type).Where(t => !string.IsNullOrEmpty(t)).Distinct());

        var totalAnnualRentalValue = await _rvCalculationResultsRepository.GetQueryable()
            .AsNoTracking()
            .Where(rcr => rcr.PropertyId == propertyId && !rcr.MarkedForDeletion)
            .SumAsync(rcr => (decimal?)rcr.AnnualRentalValue ?? 0m, cancellationToken);

        var propertyUseTypes = propertyData.Select(x => x.Type).Distinct().ToList();

        var specialTaxList = new List<TaxApplicabilityCalculationDto>();

        // EducationTaxData (TaxId = 2)
        if (taxMasterMap.TryGetValue(2, out var eduTmInfo))
        {
            var eduMasters = await _educationTaxMasterRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var matchedEdu = eduMasters
                .Where(e => totalAnnualRentalValue >= (e.MinAmount ?? 0m) &&
                            totalAnnualRentalValue <= (e.MaxAmount ?? decimal.MaxValue) &&
                            e.Type != null && propertyUseTypes.Contains(e.Type))
                .ToList();

            var avgTaxPercentage = matchedEdu.Any() ? matchedEdu.Average(e => e.Rate) : null;
            var baseTypes = matchedEdu.Any() ? string.Join(", ", matchedEdu.Select(e => e.OnRVOrALV).Where(b => !string.IsNullOrEmpty(b)).Distinct()) : null;

            specialTaxList.Add(new TaxApplicabilityCalculationDto
            {
                TaxId = 2,
                TaxName = eduTmInfo.TaxName,
                CalculationModeId = eduTmInfo.CalculationModeId,
                CalculationMode = eduTmInfo.CalculationModeId == 1 ? "VALUE_BASED" : "SPECIAL",
                RuleDefinitionId = eduTmInfo.RuleDefinitionId,
                TypeOfUseIds = defaultTypeOfUseIds,
                Descriptions = defaultDescriptions,
                Types = defaultTypes,
                BaseTypes = baseTypes,
                AverageTaxPercentage = avgTaxPercentage,
                MappingData = null,
                TaxAmount = taxTransactions.TryGetValue(2, out var amt) ? amt : 0m
            });
        }

        // EmploymentTaxData (TaxId = 3)
        if (taxMasterMap.TryGetValue(3, out var empTmInfo))
        {
            var empMasters = await _employmentTaxMasterRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var matchedEmp = empMasters
                .Where(e => totalAnnualRentalValue >= (e.MinAmount ?? 0m) &&
                            totalAnnualRentalValue <= (e.MaxAmount ?? decimal.MaxValue) &&
                            e.Type != null && propertyUseTypes.Contains(e.Type))
                .ToList();

            var avgTaxPercentage = matchedEmp.Any() ? matchedEmp.Average(e => e.Rate) : null;
            var baseTypes = matchedEmp.Any() ? string.Join(", ", matchedEmp.Select(e => e.OnRVOrALV).Where(b => !string.IsNullOrEmpty(b)).Distinct()) : null;

            specialTaxList.Add(new TaxApplicabilityCalculationDto
            {
                TaxId = 3,
                TaxName = empTmInfo.TaxName,
                CalculationModeId = empTmInfo.CalculationModeId,
                CalculationMode = empTmInfo.CalculationModeId == 1 ? "VALUE_BASED" : "SPECIAL",
                RuleDefinitionId = empTmInfo.RuleDefinitionId,
                TypeOfUseIds = defaultTypeOfUseIds,
                Descriptions = defaultDescriptions,
                Types = defaultTypes,
                BaseTypes = baseTypes,
                AverageTaxPercentage = avgTaxPercentage,
                MappingData = null,
                TaxAmount = taxTransactions.TryGetValue(3, out var amt) ? amt : 0m
            });
        }

        // 8. ConditionBasedTax (CalculationModeId = 2)
        var conditionRulesQuery = _taxConditionRuleRepository.GetQueryable()
            .AsNoTracking()
            .Where(tcr => tcr.IsActive);

        if (assessmentYearRangeId.HasValue && assessmentYearRangeId.Value > 0)
        {
            conditionRulesQuery = conditionRulesQuery.Where(tcr => tcr.AssessmentYearRangeId == assessmentYearRangeId.Value);
        }

        var conditionRules = await conditionRulesQuery.ToListAsync(cancellationToken);

        var conditionTaxList = taxMasterData
            .Where(tm => tm.CalculationModeId == 2)
            .Select(tm =>
            {
                var taxRules = conditionRules.Where(r => r.TaxId == tm.TaxId).ToList();

                var resultModes = string.Join(", ", taxRules.Select(r => r.ResultMode).Where(rm => !string.IsNullOrEmpty(rm)).Distinct());
                var resultBases = string.Join(", ", taxRules.Select(r => r.ResultBase).Where(rb => !string.IsNullOrEmpty(rb)).Distinct());
                var resultValues = string.Join(", ", taxRules.Select(r => r.ResultValue.ToString()).Distinct());

                return new TaxApplicabilityCalculationDto
                {
                    TaxId = tm.TaxId,
                    TaxName = tm.TaxName,
                    CalculationModeId = tm.CalculationModeId,
                    CalculationMode = "CONDITION_BASED",
                    RuleDefinitionId = tm.RuleDefinitionId,
                    ResultModes = string.IsNullOrEmpty(resultModes) ? null : resultModes,
                    ResultBases = string.IsNullOrEmpty(resultBases) ? null : resultBases,
                    ResultValues = string.IsNullOrEmpty(resultValues) ? null : resultValues,
                    MappingData = null,
                    TaxAmount = taxTransactions.TryGetValue(tm.TaxId, out var amt) ? amt : 0m
                };
            }).ToList();

        // 9. MasterBasedTax (CalculationModeId = 3)
        var masterMappingsQuery = _taxMasterMappingRepository.GetQueryable()
            .AsNoTracking()
            .Where(tmm => tmm.IsActive);

        if (assessmentYearRangeId.HasValue && assessmentYearRangeId.Value > 0)
        {
            masterMappingsQuery = masterMappingsQuery.Where(tmm => tmm.AssessmentYearRangeId == assessmentYearRangeId.Value);
        }

        var masterMappings = await masterMappingsQuery.ToListAsync(cancellationToken);

        var masterBasedTaxList = taxMasterData
            .Where(tm => tm.CalculationModeId == 3)
            .Select(tm =>
            {
                var taxMappings = masterMappings.Where(m => m.TaxId == tm.TaxId).ToList();

                var resultModes = string.Join(", ", taxMappings.Select(m => m.ResultMode).Where(rm => !string.IsNullOrEmpty(rm)).Distinct());
                var resultBases = string.Join(", ", taxMappings.Select(m => m.ResultBase).Where(rb => !string.IsNullOrEmpty(rb)).Distinct());
                var resultValues = string.Join(", ", taxMappings.Select(m => m.ResultValue.ToString()).Distinct());

                return new TaxApplicabilityCalculationDto
                {
                    TaxId = tm.TaxId,
                    TaxName = tm.TaxName,
                    CalculationModeId = tm.CalculationModeId,
                    CalculationMode = "MASTER_BASED",
                    RuleDefinitionId = tm.RuleDefinitionId,
                    ResultModes = string.IsNullOrEmpty(resultModes) ? null : resultModes,
                    ResultBases = string.IsNullOrEmpty(resultBases) ? null : resultBases,
                    ResultValues = string.IsNullOrEmpty(resultValues) ? null : resultValues,
                    MappingData = null,
                    TaxAmount = taxTransactions.TryGetValue(tm.TaxId, out var amt) ? amt : 0m
                };
            }).ToList();

        // 10. COMBINE & ORDER BY TaxId (Filter strictly to TaxIds present in PTIS.TransMast for this property)
        var propertyTaxIds = taxTransactions.Keys.ToHashSet();

        var calculationResult = valueBasedTaxList
            .Concat(specialTaxList)
            .Concat(conditionTaxList)
            .Concat(masterBasedTaxList)
            .Where(x => propertyTaxIds.Contains(x.TaxId))
            .GroupBy(x => x.TaxId)
            .Select(g => g.First())
            .ToList();

        var processedTaxIds = calculationResult.Select(x => x.TaxId).ToHashSet();
        var missingTaxIds = propertyTaxIds.Except(processedTaxIds).ToList();

        foreach (var missingTaxId in missingTaxIds)
        {
            if (taxMasterMap.TryGetValue(missingTaxId, out var tmInfo))
            {
                var calcModeStr = tmInfo.CalculationModeId switch
                {
                    1 => "VALUE_BASED",
                    2 => "CONDITION_BASED",
                    3 => "MASTER_BASED",
                    _ => "OTHER"
                };

                calculationResult.Add(new TaxApplicabilityCalculationDto
                {
                    TaxId = missingTaxId,
                    TaxName = tmInfo.TaxName,
                    CalculationModeId = tmInfo.CalculationModeId,
                    CalculationMode = calcModeStr,
                    RuleDefinitionId = tmInfo.RuleDefinitionId,
                    IsApplicable = true,
                    IsActive = tmInfo.IsActive,
                    AssessmentStatus = tmInfo.AssessmentStatus,
                    TaxAmount = taxTransactions.TryGetValue(missingTaxId, out var amt) ? amt : 0m
                });
            }
        }

        calculationResult = calculationResult.OrderBy(x => x.TaxId).ToList();

        foreach (var item in calculationResult)
        {
            if (string.IsNullOrEmpty(item.TypeOfUseIds))
                item.TypeOfUseIds = defaultTypeOfUseIds;

            if (string.IsNullOrEmpty(item.Descriptions))
                item.Descriptions = defaultDescriptions;

            if (string.IsNullOrEmpty(item.Types))
                item.Types = defaultTypes;

            // Mark IsApplicable = false if tax exists in ApplyTaxesMaster with IsActive == true
            item.IsApplicable = !exemptedTaxIdSet.Contains(item.TaxId);
        }

        response.ApplicableCount = calculationResult.Count(x => x.IsApplicable);
        response.ExemptedCount = calculationResult.Count(x => !x.IsApplicable);

        response.TaxCalculations = calculationResult;

        // Populate Summary header metrics
        var fullPropertyDetails = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.PropertyId == propertyId && !pd.MarkedForDeletion)
            .Include(pd => pd.TypeOfUse)
            .ToListAsync(cancellationToken);

        var summaryPropertyDetailsIds = fullPropertyDetails.Select(pd => pd.Id).ToList();
        var summaryRvData = await _rvCalculationResultsRepository.GetQueryable()
            .Where(rcr => summaryPropertyDetailsIds.Contains(rcr.PropertyDetailsId) && !rcr.MarkedForDeletion)
            .GroupBy(rcr => rcr.PropertyDetailsId)
            .Select(g => new
            {
                PropertyDetailsId = g.Key,
                RateableValue = g.Max(rcr => rcr.RateableValue ?? 0m)
            })
            .ToDictionaryAsync(x => x.PropertyDetailsId, x => x.RateableValue, cancellationToken);

        decimal resRv = 0m;
        decimal commRv = 0m;

        foreach (var pd in fullPropertyDetails)
        {
            var pdRv = summaryRvData.TryGetValue(pd.Id, out var val) ? val : 0m;
            var typeStr = (pd.TypeOfUse?.Type ?? pd.TypeOfUse?.Description ?? "").Trim().ToUpperInvariant();
            if (typeStr.StartsWith("R") || typeStr.Contains("RESIDENT"))
            {
                resRv += pdRv;
            }
            else
            {
                commRv += pdRv;
            }
        }

        double totalArea = fullPropertyDetails.Sum(pd => pd.BuiltupAreaSqMeter ?? pd.CarpetAreaSqMeter ?? 0);

        var toiletSocialDetails = await _propertySocialDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(psd => psd.PropertyId == propertyId && !psd.MarkedForDeletion && (psd.SocialAttributeId == 1051 || psd.SocialAttributeId == 1053))
            .ToListAsync(cancellationToken);

        int totalToilets = toiletSocialDetails.Sum(psd => psd.IntValue ?? 0);
        decimal totalTaxAmount = calculationResult.Sum(t => t.TaxAmount ?? 0m);

        response.Summary = new TaxApplicabilityHeaderSummaryDto
        {
            TotalTax = totalTaxAmount,
            ResidentialRV = resRv,
            CommercialRV = commRv,
            Area = totalArea,
            Toilets = totalToilets
        };

        var applicabilityQuery = from tm in _taxMasterRepository.GetQueryable().Where(x => x.IsActive)
                                   join tpr in _taxPercentageRVRepository.GetQueryable().Where(x => x.IsActive)
                                       on tm.Id equals tpr.TaxId into tprGroup
                                   from tpr in tprGroup.DefaultIfEmpty()
                                   join app in _repository.GetQueryable().Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                                       on tm.Id equals app.TaxId into appGroup
                                   from app in appGroup.DefaultIfEmpty()
                                   group new { tm, tpr, app } by new
                                   {
                                       tm.Id,
                                       tm.TaxName,
                                       tm.TaxCode,
                                       tm.DisplayOrder,
                                       tm.IsActive,
                                       tm.AssessmentStatus
                                   } into g
                                   orderby g.Key.DisplayOrder
                                   select new
                                   {
                                       TaxId = g.Key.Id,
                                       IsApplicable = g.Any(x => x.tpr != null) && !g.Any(x => x.app != null),
                                       IsActive = g.Key.IsActive,
                                       AssessmentStatus = g.Key.AssessmentStatus
                                   };

        var applicabilityRaw = await applicabilityQuery.ToListAsync(cancellationToken);
        var applicabilityMap = applicabilityRaw.ToDictionary(t => t.TaxId);

        foreach (var calcItem in calculationResult)
        {
            if (applicabilityMap.TryGetValue(calcItem.TaxId, out var detail))
            {
                calcItem.IsApplicable = detail.IsApplicable;
                calcItem.IsActive = detail.IsActive;
                calcItem.AssessmentStatus = detail.AssessmentStatus;
            }
            else
            {
                calcItem.IsApplicable = true;
                calcItem.IsActive = true;
                calcItem.AssessmentStatus = true;
            }
        }

        response.ApplicableCount = applicabilityRaw.Count(t => t.IsApplicable);
        response.ExemptedCount = applicabilityRaw.Count(t => !t.IsApplicable);

        return response;
    }
}