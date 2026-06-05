using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyRuleEvaluationMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Master
{
    public class PropertyRuleEvaluationMasterService : BaseCommonCrudService<PropertyRuleEvaluationMasterEntity, PropertyRuleEvaluationMasterDto, CreatePropertyRuleEvaluationMasterDto, UpdatePropertyRuleEvaluationMasterDto, PropertyRuleEvaluationMasterQueryParameters, int>, IPropertyRuleEvaluationMasterService
    {
        public PropertyRuleEvaluationMasterService(
            IRepository<PropertyRuleEvaluationMasterEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
