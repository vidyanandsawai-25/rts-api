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

    public TaxApplicabilityService(
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<TaxPercentageMasterRVEntity, int> taxPercentageRVRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<AssessmentYearRangeEntity, int> yearRangeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
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
}