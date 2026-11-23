using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ApprovalFlowMaster CRUD operations
/// </summary>
public class RTSApprovalFlowMasterService : BaseCommonCrudService<RTSApprovalFlowMasterEntity, RTSApprovalFlowMasterDto, CreateRTSApprovalFlowMasterDto, UpdateRTSApprovalFlowMasterDto, RTSApprovalFlowMasterQueryParameters, int>, IRTSApprovalFlowMasterService
{
    public RTSApprovalFlowMasterService(
        IRepository<RTSApprovalFlowMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

/// <summary>
/// Service for ApprovalFlowStageMaster CRUD operations
/// </summary>
public class RTSApprovalFlowStageMasterService : BaseCommonCrudService<RTSApprovalFlowStageMasterEntity, RTSApprovalFlowStageMasterDto, CreateRTSApprovalFlowStageMasterDto, UpdateRTSApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterQueryParameters, int>, IRTSApprovalFlowStageMasterService
{
    private readonly IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> _stageUserAllocationRepository;
    private readonly IRepository<RTSApplicationDetailsEntity, int> _applicationRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;

    public RTSApprovalFlowStageMasterService(
        IRepository<RTSApprovalFlowStageMasterEntity, int> repository,
        IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> stageUserAllocationRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        IRepository<TrackApplicationHistoryEntity, int> historyRepository)
        : base(repository, unitOfWork, mapper)
    {
        _stageUserAllocationRepository = stageUserAllocationRepository;
        _applicationRepository = applicationRepository;
        _historyRepository = historyRepository;
    }

    public override async Task<PagedResult<RTSApprovalFlowStageMasterDto>> GetAllAsync(
        RTSApprovalFlowStageMasterQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        await PopulateUserAllocationCountsAsync(result.Items, cancellationToken);
        return result;
    }

    public override async Task<RTSApprovalFlowStageMasterDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GetByIdAsync(id, cancellationToken);
        if (result is not null)
        {
            await PopulateUserAllocationCountsAsync(new[] { result }, cancellationToken);
        }
        return result;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var stage = await _repository.GetByIdAsync(id, cancellationToken);
        if (stage is null) return false;

        var validationResult = await ValidateForDeleteAsync(id, stage, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for delete operation";
            throw new NtisPlatform.Application.Exceptions.ValidationException(
                message, validationResult.ToDictionary(), NtisPlatform.Application.Enums.OperationType.Delete);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var hasApplications = await _applicationRepository.GetQueryable()
                .AnyAsync(application => application.CurrentApprovalFlowStageId == id, cancellationToken);
            var hasHistory = await _historyRepository.GetQueryable()
                .AnyAsync(history => history.ApprovalFlowStageId == id, cancellationToken);
            if (hasApplications || hasHistory)
            {
                throw new NtisPlatform.Application.Exceptions.ValidationException(
                    "Cannot delete this stage because it is referenced by an application or its history.",
                    NtisPlatform.Application.Enums.OperationType.Delete);
            }

            var allocations = await _stageUserAllocationRepository.GetQueryable()
                .Where(allocation => allocation.ApprovalFlowStageId == id)
                .ToListAsync(cancellationToken);
            foreach (var allocation in allocations)
            {
                await _stageUserAllocationRepository.HardDeleteAsync(allocation, cancellationToken);
            }

            await _repository.HardDeleteAsync(stage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    /// <summary>
    /// Enriches a returned page of stages with allocation counts in one grouped query.
    /// Allocation rows are counted whether active or inactive because both remain assigned
    /// to the stage configuration.
    /// </summary>
    private async Task PopulateUserAllocationCountsAsync(
        IEnumerable<RTSApprovalFlowStageMasterDto> stages,
        CancellationToken cancellationToken)
    {
        var stageRows = stages as ICollection<RTSApprovalFlowStageMasterDto> ?? stages.ToList();
        if (stageRows.Count == 0) return;

        var stageIds = stageRows.Select(stage => stage.Id).Distinct().ToList();
        var countsByStageId = await _stageUserAllocationRepository.GetQueryable()
            .AsNoTracking()
            .Where(allocation =>
                allocation.ApprovalFlowStageId.HasValue &&
                stageIds.Contains(allocation.ApprovalFlowStageId.Value))
            .GroupBy(allocation => allocation.ApprovalFlowStageId!.Value)
            .Select(group => new { StageId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.StageId, item => item.Count, cancellationToken);

        foreach (var stage in stageRows)
        {
            stage.UserAllocationCount = countsByStageId.GetValueOrDefault(stage.Id);
        }
    }
}
