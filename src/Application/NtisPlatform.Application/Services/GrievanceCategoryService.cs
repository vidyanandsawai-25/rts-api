using AutoMapper;
using NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class GrievanceCategoryService : BaseCommonCrudService<GrievanceCategoryEntity, GrievanceCategoryDto, CreateGrievanceCategoryDto, UpdateGrievanceCategoryDto, GrievanceCategoryQueryParameters, int>, IGrievanceCategoryService
    {
        public GrievanceCategoryService(IRepository<GrievanceCategoryEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
