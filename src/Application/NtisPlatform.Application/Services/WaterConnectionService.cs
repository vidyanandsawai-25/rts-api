using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterConnectionService
    : BaseCommonCrudService<WaterConnectionMasterEntity, WaterConnectionDto, CreateWaterConnectionDto, UpdateWaterConnectionDto, WaterConnectionQueryParameters, int>,
      IWaterConnectionService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<WaterRateMasterEntity, int> _rateRepository;
    private readonly IRepository<YearMasterEntity, int> _yearRepository;

    public WaterConnectionService(
        IRepository<WaterConnectionMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<WaterRateMasterEntity, int> rateRepository,
        IRepository<YearMasterEntity, int> yearRepository)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _rateRepository = rateRepository;
        _yearRepository = yearRepository;
    }

    protected override IQueryable<WaterConnectionMasterEntity> ApplyIncludes(
        IQueryable<WaterConnectionMasterEntity> query)
        => query
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.WaterConnectionStatus);

    // Base GetById — uses auto-detected current FY (no financeYearId param available on base signature)
    public override async Task<WaterConnectionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await GetByIdWithFinanceYearAsync(id, financeYearId: null, cancellationToken);

    // Extended GetById — caller may supply a specific financeYearId
    public async Task<WaterConnectionDto?> GetByIdWithFinanceYearAsync(
        int id,
        int? financeYearId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.WaterConnectionStatus)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null) return null;

        var dto = _mapper.Map<WaterConnectionDto>(entity);
        await PopulateRateFieldsAsync(new[] { dto }, financeYearId, cancellationToken);
        return dto;
    }

    // GetAll — forwards WaterConnectionQueryParameters.FinanceYearId to the rate lookup
    public override async Task<PagedResult<WaterConnectionDto>> GetAllAsync(
        WaterConnectionQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        await PopulateRateFieldsAsync(result.Items, queryParameters.FinanceYearId, cancellationToken);
        return result;
    }

    /// <summary>
    /// Resolves the financial year (by id if supplied, otherwise current date), fetches all matching
    /// active rate rows in one query, then sets ApplicableRate and ApplicableCharges on every DTO.
    ///
    /// ApplicableRate  = WaterRateMaster.YearlyRate
    /// ApplicableCharges = ROUND((YearlyRate / 12) * ChargeMonths, 2)
    ///   where ChargeMonths counts months from MAX(ConnectionStartDate, FYStart)
    ///                                          to MIN(ConnectionStopDate ?? FYEnd, FYEnd) inclusive.
    /// </summary>
    private async Task PopulateRateFieldsAsync(
        IEnumerable<WaterConnectionDto> dtos,
        int? financeYearId,
        CancellationToken cancellationToken)
    {
        var dtoList = dtos.ToList();
        if (dtoList.Count == 0) return;

        YearMasterEntity? year;
        if (financeYearId.HasValue)
        {
            year = await _yearRepository.GetByIdAsync(financeYearId.Value, cancellationToken);
        }
        else
        {
            var today = DateTime.Today;
            year = await _yearRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    y => y.IsActive && y.StartDate <= today && y.EndDate >= today,
                    cancellationToken);
        }

        if (year?.StartDate == null || year.EndDate == null) return;

        var fyStart = year.StartDate.Value.Date;
        var fyEnd   = year.EndDate.Value.Date;

        var rates = await _rateRepository.GetQueryable()
            .AsNoTracking()
            .Where(r => r.FinanceYearId == year.Id && r.IsActive)
            .ToListAsync(cancellationToken);

        if (rates.Count == 0) return;

        var rateDict = rates.ToDictionary(r => (r.WaterConnectionTypeId, r.WaterConnectionSizeId));

        foreach (var dto in dtoList)
        {
            if (!rateDict.TryGetValue((dto.WaterConnectionTypeId, dto.WaterConnectionSizeId), out var rate))
                continue;

            // Connection has no overlap with this FY
            if (dto.ConnectionStopDate.HasValue && dto.ConnectionStopDate.Value.Date < fyStart) continue;
            if (dto.ConnectionStartDate.Date > fyEnd) continue;

            dto.ApplicableRate = rate.YearlyRate;

            // Pro-rata bill: same formula as WaterConnectionDetailsService.GenerateBillAsync
            var chargeStart = dto.ConnectionStartDate.Date > fyStart
                ? dto.ConnectionStartDate.Date
                : fyStart;

            var chargeEnd = dto.ConnectionStopDate.HasValue && dto.ConnectionStopDate.Value.Date < fyEnd
                ? dto.ConnectionStopDate.Value.Date
                : fyEnd;

            var chargeMonths = (chargeEnd.Year - chargeStart.Year) * 12
                             + chargeEnd.Month - chargeStart.Month + 1;

            if (chargeMonths > 0)
                dto.ApplicableCharges = Math.Round(rate.YearlyRate / 12m * chargeMonths, 2);
        }
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterConnectionMasterEntity currentEntity,
        WaterConnectionMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterConnectionMasterEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterConnectionMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterConnectionMasterEntity>(id, cancellationToken);
    }
}
