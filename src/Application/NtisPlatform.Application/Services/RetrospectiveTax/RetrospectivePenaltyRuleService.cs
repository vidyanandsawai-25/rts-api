using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectivePenaltyRuleService : BaseCommonCrudService<RetrospectivePenaltyRuleEntity, RetrospectivePenaltyRuleDto, CreateRetrospectivePenaltyRuleDto, UpdateRetrospectivePenaltyRuleDto, RetrospectivePenaltyRuleQueryParameters, int>, IRetrospectivePenaltyRuleService
{
    public RetrospectivePenaltyRuleService(
        IRepository<RetrospectivePenaltyRuleEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
