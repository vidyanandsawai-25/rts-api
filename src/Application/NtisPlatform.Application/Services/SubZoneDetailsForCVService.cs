using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class SubZoneDetailsForCVService : BaseCommonCrudService<SubZoneDetailsForCVEntity, SubZoneDetailsForCVDto, CreateSubZoneDetailsForCVDto, UpdateSubZoneDetailsForCVDto, SubZoneDetailsForCVQueryParameters, int>, ISubZoneDetailsForCVService
{
    public SubZoneDetailsForCVService(
        IRepository<SubZoneDetailsForCVEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
