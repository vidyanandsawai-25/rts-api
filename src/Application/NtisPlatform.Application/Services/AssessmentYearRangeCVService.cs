using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class AssessmentYearRangeCVService : BaseCommonCrudService<AssessmentYearRangeCVEntity, AssessmentYearRangeCVDto, CreateAssessmentYearRangeCVDto, UpdateAssessmentYearRangeCVDto, AssessmentYearRangeCVQueryParameters, int>, IAssessmentYearRangeCVService
    {
        public AssessmentYearRangeCVService(IRepository<AssessmentYearRangeCVEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
