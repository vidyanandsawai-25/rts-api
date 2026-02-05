using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class MoujaService : BaseCommonCrudService<MoujaEntity, MoujaDto, CreateMoujaDto, UpdateMoujaDto, MoujaQueryParameters, int>, IMoujaService
{
    public MoujaService(
        IRepository<MoujaEntity, int> repository,
        IUnitOfWork unitOfWork,
         IMapper mapper)
        : base(repository, unitOfWork, mapper) { }
}
