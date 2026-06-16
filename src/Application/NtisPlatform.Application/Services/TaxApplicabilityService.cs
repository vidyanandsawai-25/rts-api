using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

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

    public TaxApplicabilityService(
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<TaxPercentageMasterRVEntity, int> taxPercentageRVRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<AssessmentYearRangeEntity, int> yearRangeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
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
        // Get calculated tax amounts from TransMast - this is the source of truth for which taxes to display
        var taxAmounts = await _transMastRepository
            .GetQueryable()
            .Where(tm => tm.PropertyId == request.PropertyId 
                      && tm.FinanceYearId == request.FinancialYearId
                      && tm.RVorCV == request.RvOrCv.Trim().ToUpperInvariant()
                      && tm.IsActive 
                      && !tm.MarkedForDeletion)
            .OrderBy(tm => tm.TaxId)
            .ToListAsync(cancellationToken);

        // If no TransMast records found, return empty response (not null)
        var response = new TaxApplicabilityResponseDto
        {
            PropertyId = request.PropertyId,
            FinancialYearId = request.FinancialYearId,
            TypeOfUseGroupId = request.TypeOfUseGroupId
        };

        // If no tax amounts found in TransMast, return empty lists
        if (taxAmounts == null || !taxAmounts.Any())
        {
            return response;
        }

        // Fetch all ApplyTaxesMaster records for this property
        var applyTaxesMasterList = await _repository
            .GetQueryable()
            .Where(x => x.PropertyId == request.PropertyId)
            .ToListAsync(cancellationToken);

        // Get all tax master details for the taxes in TransMast
        var taxIds = taxAmounts.Select(ta => ta.TaxId).Distinct().ToList();
        var taxMasterList = await _taxMasterRepository
            .GetQueryable()
            .Where(t => taxIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        // Get all active year ranges to find the correct YearRangeRVId
        var yearRanges = await _yearRangeRepository
            .GetQueryable()
            .Where(yr => yr.IsActive)
            .ToListAsync(cancellationToken);

        var yearRange = yearRanges.FirstOrDefault();
        var yearRangeRVId = yearRange?.Id ?? 0;

        // Get all TypeOfUse IDs belonging to the specified TypeOfUseGroupId
        var typeOfUseIds = await _typeOfUseRepository
            .GetQueryable()
            .Where(t => t.TypeOfUseGroupId == request.TypeOfUseGroupId && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        // Get tax percentage configurations for all TypeOfUse IDs in the group and YearRangeRVId
        var taxPercentages = await _taxPercentageRVRepository
            .GetQueryable()
            .Where(tp => typeOfUseIds.Contains(tp.TypeOfUseId) 
                      && tp.YearRangeRVId == yearRangeRVId
                      && tp.IsActive)
            .ToListAsync(cancellationToken);

        // Pre-build dictionaries to optimize search inside the loop from O(n^2) to O(n)
        var taxMasterDict = taxMasterList.ToDictionary(t => t.Id);
        var taxPercentageDict = taxPercentages.GroupBy(tp => tp.TaxId).ToDictionary(g => g.Key, g => g.First());
        var applyTaxesDict = applyTaxesMasterList.GroupBy(x => x.TaxId).ToDictionary(g => g.Key, g => g.First());

        // Process each tax from TransMast and categorize as applicable or exempted
        foreach (var taxAmount in taxAmounts)
        {
            // Get tax master details
            taxMasterDict.TryGetValue(taxAmount.TaxId, out var taxMaster);
            
            // Get tax percentage for this tax (take the first matching percentage if multiple exist)
            taxPercentageDict.TryGetValue(taxAmount.TaxId, out var taxPercentage);

            // Determine applicability status: if record exists in ApplyTaxesMaster, use the inversion of its IsActive status (since IsActive=1 means active exemption/disabled), otherwise use TaxMaster.IsActive
            applyTaxesDict.TryGetValue(taxAmount.TaxId, out var applyTaxRecord);
            bool isApplicableState = applyTaxRecord != null ? !applyTaxRecord.IsActive : (taxMaster?.IsActive ?? false);

            var taxDetail = new TaxApplicabilityDetailDto
            {
                TaxId = taxAmount.TaxId,
                TaxHead = taxMaster?.TaxName ?? "Unknown Tax",
                TaxCode = taxMaster?.TaxCode ?? "",
                CalculationType = null,
                TaxPercentage = taxPercentage?.TaxPercentage ?? 0,
                TaxAmount = taxAmount.TaxAmount,
                IsActive = isApplicableState,
                IsApplicable = isApplicableState
            };

            // Classify tax as applicable or exempted based on applicability (which mirrors active status)
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
}