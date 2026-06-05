using NtisPlatform.Application.DTOs.Master.PropertyRuleEvaluationMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IPropertyRuleEvaluationMasterService : ICommonCrudService<PropertyRuleEvaluationMasterEntity, PropertyRuleEvaluationMasterDto, CreatePropertyRuleEvaluationMasterDto, UpdatePropertyRuleEvaluationMasterDto, PropertyRuleEvaluationMasterQueryParameters, int>
    {
    }
}
