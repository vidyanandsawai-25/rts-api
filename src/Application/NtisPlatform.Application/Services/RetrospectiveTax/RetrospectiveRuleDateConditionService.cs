using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleDateConditionService : BaseCommonCrudService<RetrospectiveRuleDateConditionEntity, RetrospectiveRuleDateConditionDto, CreateRetrospectiveRuleDateConditionDto, UpdateRetrospectiveRuleDateConditionDto, RetrospectiveRuleDateConditionQueryParameters, int>, IRetrospectiveRuleDateConditionService
{
    public RetrospectiveRuleDateConditionService(
        IRepository<RetrospectiveRuleDateConditionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
