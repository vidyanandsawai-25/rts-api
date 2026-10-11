using AutoMapper;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSRuleMasterService : BaseCommonCrudService<RTSRuleMasterEntity, RTSRuleMasterDto, CreateRTSRuleMasterRequestDto, UpdateRTSRuleMasterRequestDto, RTSRuleMasterQueryParameters, int>, IRTSRuleMasterService
{

    public RTSRuleMasterService(
        IRepository<RTSRuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
    }


    public override async Task<BulkResult<RTSRuleMasterDto>> BulkCreateAsync(CreateRTSRuleMasterRequestDto[] items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Length == 0)
            return new BulkResult<RTSRuleMasterDto>(0, 0, []);

        var entities = new List<RTSRuleMasterEntity>();

        foreach (var batch in items)
        {
            if (batch.Rules == null || batch.Rules.Count == 0) continue;

            var mapped = _mapper.Map<List<RTSRuleMasterEntity>>(batch.Rules);
            foreach (var e in mapped)
            {
                e.ServiceId = batch.ServiceId;
                e.CreatedBy = batch.CreatedBy;
                e.CreatedDate = DateTime.UtcNow;
                e.IsActive = true;
            }

            entities.AddRange(mapped);
        }

        if (entities.Count > 0)
        {
            await _repository.AddRangeAsync(entities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dtos = _mapper.Map<List<RTSRuleMasterDto>>(entities);
        return new BulkResult<RTSRuleMasterDto>(dtos.Count, 0, dtos);
    }


    public override async Task<RTSRuleMasterDto?> UpdateAsync(int serviceId, UpdateRTSRuleMasterRequestDto updateDto,CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0)
            throw new ArgumentException("Service ID must be greater than zero.", nameof(serviceId));

        if (updateDto.Rules == null || updateDto.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required for update.");

        RTSRuleMasterEntity? firstUpdated = null;

        foreach (var ruleDto in updateDto.Rules)
        {
            if (!ruleDto.Id.HasValue || ruleDto.Id.Value <= 0)
                continue;

            var entity = await _repository.GetByIdAsync(ruleDto.Id.Value, cancellationToken);
            if (entity == null) continue;

            _mapper.Map(ruleDto, entity);
            entity.ServiceId = serviceId;
            entity.UpdatedBy = updateDto.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;

            await _repository.UpdateAsync(entity, cancellationToken);
            firstUpdated ??= entity;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return firstUpdated == null ? null : _mapper.Map<RTSRuleMasterDto>(firstUpdated);
    }
}
