using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleAuditLogService : BaseCommonCrudService<RetrospectiveRuleAuditLogEntity, RetrospectiveRuleAuditLogDto, CreateRetrospectiveRuleAuditLogDto, UpdateRetrospectiveRuleAuditLogDto, RetrospectiveRuleAuditLogQueryParameters, long>, IRetrospectiveRuleAuditLogService
{
    public RetrospectiveRuleAuditLogService(
        IRepository<RetrospectiveRuleAuditLogEntity, long> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
