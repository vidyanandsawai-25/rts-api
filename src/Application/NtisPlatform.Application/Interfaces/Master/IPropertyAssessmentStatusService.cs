using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPropertyAssessmentStatusService
    : ICommonCrudService<PropertyAssessmentStatusEntity, PropertyAssessmentStatusDto, CreatePropertyAssessmentStatusDto, UpdatePropertyAssessmentStatusDto, PropertyAssessmentStatusQueryParameters, int>
{
}
