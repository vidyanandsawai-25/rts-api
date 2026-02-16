using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.AssessmentYearRange;
using NtisPlatform.Core.Entities.Master;


namespace NtisPlatform.Application.Interfaces
{
    public interface IAssessmentYearRangeService : ICommonCrudService<AssessmentYearRangeEntity, AssessmentYearRangeDto, CreateAssessmentYearRangeDto, UpdateAssessmentYearRangeDto, AssessmentYearRangeQueryParameters, int>
    {
    }
}
