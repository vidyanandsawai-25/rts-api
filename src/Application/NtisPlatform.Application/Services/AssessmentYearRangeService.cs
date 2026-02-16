using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.AssessmentYearRange;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class AssessmentYearRangeService : BaseCommonCrudService<AssessmentYearRangeEntity, AssessmentYearRangeDto, CreateAssessmentYearRangeDto, UpdateAssessmentYearRangeDto, AssessmentYearRangeQueryParameters, int>, IAssessmentYearRangeService
    {
        public AssessmentYearRangeService(IRepository<AssessmentYearRangeEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
