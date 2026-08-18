using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleSummaryService : BaseCommonCrudService<RetrospectiveRuleSummaryEntity, RetrospectiveRuleSummaryDto, CreateRetrospectiveRuleSummaryDto, UpdateRetrospectiveRuleSummaryDto, RetrospectiveRuleSummaryQueryParameters, int>, IRetrospectiveRuleSummaryService
{
    private readonly IRepository<RetrospectiveRuleMasterEntity, int> _ruleRepository;

    public RetrospectiveRuleSummaryService(
        IRepository<RetrospectiveRuleSummaryEntity, int> repository,
        IRepository<RetrospectiveRuleMasterEntity, int> ruleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _ruleRepository = ruleRepository;
    }

    public async Task<RetrospectiveRuleSummaryViewDto?> GetForRuleAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);
        if (rule is null)
            return null;

        var summary = await _repository.GetQueryable()
            .Where(s => s.RuleId == ruleId && s.IsActive)
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new RetrospectiveRuleSummaryViewDto
        {
            RuleId = ruleId,
            RuleCode = rule.RuleCode,
            WhenSummary = summary?.WhenSummary,
            TaxSummary = summary?.TaxSummary,
            PenaltySummary = summary?.PenaltySummary,
            SummaryGeneratedDate = summary?.CreatedDate
        };
    }
}
