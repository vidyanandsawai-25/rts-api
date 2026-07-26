using System;
using System.Linq;
using AutoMapper;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSApplicationService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSFieldValueQueryParameters, int>, IRTSApplicationService
{
    private readonly IRTSCitizenSessionService _sessionService;

    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRTSCitizenSessionService sessionService,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
    }

    public override async Task<RTSApplicationDetailsDto> CreateAsync(CreateRTSApplicationDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(createDto.SessionId))
        {
            var validationResult = await _sessionService.ValidateAndUpdateSessionAsync(createDto.SessionId, cancellationToken);
            if (!validationResult.Success)
            {
                throw new UnauthorizedAccessException($"CitizenSession_{validationResult.Message}");
            }
        }

        var entity = _mapper.Map<RTSApplicationDetailsEntity>(createDto);
        entity.ApplicationStatus = string.IsNullOrWhiteSpace(createDto.ApplicationStatus) || createDto.ApplicationStatus == "string" ? "Submitted" : createDto.ApplicationStatus;

        if (createDto.FieldValues?.Any() == true)
        {
            entity.FieldValueData = createDto.FieldValues
                .Select(f =>
                {
                    var field = _mapper.Map<RTSFieldValueEntity>(f);
                    field.CreatedBy = createDto.CreatedBy;
                    return field;
                })
                .ToList();
        }

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RTSApplicationDetailsDto>(entity);
    }
}
