using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ConstructionTypeService : BaseCommonCrudService<ConstructionTypeEntity, ConstructionTypeDto, CreateConstructionTypeDto, UpdateConstructionTypeDto, ConstructionTypeQueryParameters, int>, IConstructionTypeService
{
    public ConstructionTypeService(
        IRepository<ConstructionTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
