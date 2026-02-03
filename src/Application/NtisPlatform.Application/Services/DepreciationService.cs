using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;


namespace NtisPlatform.Application.Services;

public class DepreciationService(IRepository<DepreciationMasterEntity, int> repository,IUnitOfWork unitOfWork,IMapper mapper) : BaseCommonCrudService<DepreciationMasterEntity, DepreciationDtos, CreateDepreciationDto, UpdateDepreciationDto, DepreciationQueryParameters, int>
    (repository, unitOfWork, mapper), IDepreciationService
{
}

