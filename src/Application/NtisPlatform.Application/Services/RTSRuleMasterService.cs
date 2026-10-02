using AutoMapper;
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


    public async Task<RTSRuleMasterResponseDto> CreateRulesAsync(CreateRTSRuleMasterRequestDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto.Rules == null || createDto.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required.");

        var entities = _mapper.Map<List<RTSRuleMasterEntity>>(createDto.Rules);


        foreach (var entity in entities)
        {
            entity.ServiceId = createDto.ServiceId;
            entity.CreatedBy = createDto.CreatedBy;
            entity.IsActive = true;
        }

        await _repository.AddRangeAsync(entities, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RTSRuleMasterResponseDto
        {
            ServiceId = createDto.ServiceId,
            RuleCount = entities.Count,
            Rules = _mapper.Map<List<RTSRuleMasterDto>>(entities)
        };
    }


    public async Task<RTSRuleMasterResponseDto> UpdateRulesAsync(int serviceId, UpdateRTSRuleMasterRequestDto updateDto, CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0)
            throw new ArgumentException("Service ID must be greater than zero.", nameof(serviceId));

        if (updateDto.Rules == null || updateDto.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required for update.");

        var updatedEntities = new List<RTSRuleMasterEntity>();

        foreach (var ruleDto in updateDto.Rules)
        {
            if (ruleDto.Id.HasValue && ruleDto.Id.Value > 0)
            {
                var entity = await _repository.GetByIdAsync(ruleDto.Id.Value, cancellationToken);
                if (entity != null)
                {
                    _mapper.Map(ruleDto, entity);
                    entity.ServiceId = serviceId;
                    entity.UpdatedBy = updateDto.UpdatedBy;
                    entity.UpdatedDate = DateTime.UtcNow;

                    await _repository.UpdateAsync(entity, cancellationToken);
                    updatedEntities.Add(entity);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Returns unified batch response with updated rules
        return new RTSRuleMasterResponseDto
        {
            ServiceId = serviceId,
            RuleCount = updatedEntities.Count,
            Rules = _mapper.Map<List<RTSRuleMasterDto>>(updatedEntities)
        };
    }
}
