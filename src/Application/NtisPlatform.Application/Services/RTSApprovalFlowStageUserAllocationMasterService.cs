using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowStageUserAllocationMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSApprovalFlowStageUserAllocationMasterService : BaseCommonCrudService<
    RTSApprovalFlowStageUserAllocationMasterEntity,
    RTSApprovalFlowStageUserAllocationMasterDto,
    CreateRTSApprovalFlowStageUserAllocationMasterDto,
    UpdateRTSApprovalFlowStageUserAllocationMasterDto,
    RTSApprovalFlowStageUserAllocationMasterQueryParameters,
    int>, IRTSApprovalFlowStageUserAllocationMasterService
{
    private readonly IRepository<UserEntity, int> _userRepository;

    public RTSApprovalFlowStageUserAllocationMasterService(
        IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> repository,
        IRepository<UserEntity, int> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _userRepository = userRepository;
    }

    public override async Task<PagedResult<RTSApprovalFlowStageUserAllocationMasterDto>> GetAllAsync(
        RTSApprovalFlowStageUserAllocationMasterQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        await PopulateUserProfilesAsync(result.Items, cancellationToken);
        return result;
    }

    public override async Task<RTSApprovalFlowStageUserAllocationMasterDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await base.GetByIdAsync(id, cancellationToken);
        if (result is not null)
        {
            await PopulateUserProfilesAsync(new[] { result }, cancellationToken);
        }
        return result;
    }

    public override async Task<RTSApprovalFlowStageUserAllocationMasterDto> CreateAsync(
        CreateRTSApprovalFlowStageUserAllocationMasterDto createDto,
        CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(createDto, cancellationToken);
        await PopulateUserProfilesAsync(new[] { result }, cancellationToken);
        return result;
    }

    public override async Task<RTSApprovalFlowStageUserAllocationMasterDto?> UpdateAsync(
        int id,
        UpdateRTSApprovalFlowStageUserAllocationMasterDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var result = await base.UpdateAsync(id, updateDto, cancellationToken);
        if (result is not null)
        {
            await PopulateUserProfilesAsync(new[] { result }, cancellationToken);
        }
        return result;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation is null) return false;

        // A removed candidate must not reappear in the stage editor's unfiltered list.
        // This table has no database relationships while the allocation design is being finalized.
        await _repository.HardDeleteAsync(allocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Adds display-only officer data in one user query for the returned allocation rows.
    /// Missing users deliberately leave their profile fields null so loose historical rows remain visible.
    /// </summary>
    private async Task PopulateUserProfilesAsync(
        IEnumerable<RTSApprovalFlowStageUserAllocationMasterDto> allocations,
        CancellationToken cancellationToken)
    {
        var allocationRows = allocations as ICollection<RTSApprovalFlowStageUserAllocationMasterDto>
            ?? allocations.ToList();
        var userIds = allocationRows
            .Where(allocation => allocation.UserId is > 0)
            .Select(allocation => allocation.UserId!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count == 0) return;

        var users = await _userRepository.GetAsync(
            user => userIds.Contains(user.Id),
            cancellationToken);
        var usersById = users.ToDictionary(user => user.Id);

        foreach (var allocation in allocationRows)
        {
            if (allocation.UserId is not > 0 ||
                !usersById.TryGetValue(allocation.UserId.Value, out var user))
            {
                continue;
            }

            allocation.FirstName = user.FirstName;
            allocation.MiddleName = user.MiddleName;
            allocation.LastName = user.LastName;
            allocation.UserName = user.UserName;
        }
    }
}
