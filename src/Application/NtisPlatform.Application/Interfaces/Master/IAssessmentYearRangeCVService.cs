using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces
{
    public interface IAssessmentYearRangeCVService : ICommonCrudService<AssessmentYearRangeCVEntity, AssessmentYearRangeCVDto, CreateAssessmentYearRangeCVDto, UpdateAssessmentYearRangeCVDto, AssessmentYearRangeCVQueryParameters, int>
    {
    }
}
