using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterConnectionDetailsService
    : BaseCommonCrudService<WaterConnectionDetailsEntity, WaterConnectionDetailsDto, CreateWaterConnectionDetailsDto, UpdateWaterConnectionDetailsDto, WaterConnectionDetailsQueryParameters, int>,
      IWaterConnectionDetailsService
{
    private readonly IRepository<WaterConnectionMasterEntity, int> _connectionRepository;
    private readonly IRepository<YearMasterEntity, int> _yearRepository;
    private readonly IRepository<WaterRateMasterEntity, int> _rateRepository;

    public WaterConnectionDetailsService(
        IRepository<WaterConnectionDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<WaterConnectionMasterEntity, int> connectionRepository,
        IRepository<YearMasterEntity, int> yearRepository,
        IRepository<WaterRateMasterEntity, int> rateRepository)
        : base(repository, unitOfWork, mapper)
    {
        _connectionRepository = connectionRepository;
        _yearRepository = yearRepository;
        _rateRepository = rateRepository;
    }

    protected override IQueryable<WaterConnectionDetailsEntity> ApplyIncludes(IQueryable<WaterConnectionDetailsEntity> query)
        => query
            .Include(x => x.WaterConnection)
            .Include(x => x.FinanceYear);

    public override async Task<WaterConnectionDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.WaterConnection)
            .Include(x => x.FinanceYear)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<WaterConnectionDetailsDto>(entity);
    }

    /// <summary>
    /// Calculates and persists a pro-rata water bill for the given connection and financial year.
    ///
    /// Rules:
    ///   ChargeStartDate = MAX(ConnectionStartDate, FYStartDate)
    ///   ChargeEndDate   = MIN(ConnectionStopDate ?? FYEndDate, FYEndDate)
    ///   ChargeMonths    = inclusive month count (both start and end month counted)
    ///   WaterBill       = ROUND((YearlyRate / 12) * ChargeMonths, 2)
    ///
    /// Returns null when no bill is applicable (connection stopped before FY starts,
    /// or connection not yet started by FY end).
    /// </summary>
    public async Task<WaterConnectionDetailsDto?> GenerateBillAsync(
        int waterConnectionId,
        int financeYearId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == waterConnectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Water connection {waterConnectionId} not found.");

        var financeYear = await _yearRepository.GetByIdAsync(financeYearId, cancellationToken)
            ?? throw new InvalidOperationException($"Finance year {financeYearId} not found.");

        if (financeYear.StartDate == null || financeYear.EndDate == null)
            throw new InvalidOperationException($"Finance year {financeYearId} has no valid StartDate or EndDate.");

        var fyStart = financeYear.StartDate.Value.Date;
        var fyEnd = financeYear.EndDate.Value.Date;

        // Connection stopped before this FY → no bill
        if (connection.ConnectionStopDate.HasValue && connection.ConnectionStopDate.Value.Date < fyStart)
            return null;

        // Connection starts after this FY ends → no bill
        if (connection.ConnectionStartDate.Date > fyEnd)
            return null;

        // ChargeStartDate = later of ConnectionStartDate and FYStartDate
        var chargeStart = connection.ConnectionStartDate.Date > fyStart
            ? connection.ConnectionStartDate.Date
            : fyStart;

        // ChargeEndDate = earlier of ConnectionStopDate (if any) and FYEndDate
        var chargeEnd = connection.ConnectionStopDate.HasValue && connection.ConnectionStopDate.Value.Date < fyEnd
            ? connection.ConnectionStopDate.Value.Date
            : fyEnd;

        // Month count inclusive of both start and end month; partial months count as full
        var chargeMonths = (chargeEnd.Year - chargeStart.Year) * 12
                         + chargeEnd.Month - chargeStart.Month + 1;

        if (chargeMonths <= 0)
            return null;

        var rate = await _rateRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.WaterConnectionTypeId == connection.WaterConnectionTypeId &&
                x.WaterConnectionSizeId == connection.WaterConnectionSizeId &&
                x.FinanceYearId == financeYearId &&
                x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No active rate found for connection type {connection.WaterConnectionTypeId}, " +
                $"size {connection.WaterConnectionSizeId}, finance year {financeYearId}.");

        var waterBill = Math.Round(rate.YearlyRate / 12m * chargeMonths, 2);

        // Upsert: recalculate if a bill already exists for this connection+year
        var existing = await _repository.GetQueryable()
            .FirstOrDefaultAsync(x =>
                x.WaterConnectionId == waterConnectionId &&
                x.FinanceYearId == financeYearId, cancellationToken);

        WaterConnectionDetailsEntity entity;
        if (existing != null)
        {
            existing.BillDate = DateTime.Today;
            existing.FromDate = chargeStart;
            existing.ToDate = chargeEnd;
            existing.ChargeMonths = chargeMonths;
            existing.YearlyRate = rate.YearlyRate;
            existing.WaterBill = waterBill;
            existing.IsActive = true;
            await _repository.UpdateAsync(existing, cancellationToken);
            entity = existing;
        }
        else
        {
            entity = new WaterConnectionDetailsEntity
            {
                WaterConnectionId = waterConnectionId,
                FinanceYearId = financeYearId,
                BillDate = DateTime.Today,
                FromDate = chargeStart,
                ToDate = chargeEnd,
                ChargeMonths = chargeMonths,
                YearlyRate = rate.YearlyRate,
                WaterBill = waterBill,
                IsActive = true
            };
            await _repository.AddAsync(entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<WaterConnectionDetailsDto>(entity);
    }
}
