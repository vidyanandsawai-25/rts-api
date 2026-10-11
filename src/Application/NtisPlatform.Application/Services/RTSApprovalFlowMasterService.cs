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

    public RTSApprovalFlowStageMasterService(
        IRepository<RTSApprovalFlowStageMasterEntity, int> repository,
        IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> stageUserAllocationRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _stageUserAllocationRepository = stageUserAllocationRepository;
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
