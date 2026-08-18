using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleActionService : BaseCommonCrudService<RetrospectiveRuleActionEntity, RetrospectiveRuleActionDto, CreateRetrospectiveRuleActionDto, UpdateRetrospectiveRuleActionDto, RetrospectiveRuleActionQueryParameters, int>, IRetrospectiveRuleActionService
{
    private readonly IRepository<EvidenceTypeMasterEntity, int> _evidenceTypeRepository;

    public RetrospectiveRuleActionService(
        IRepository<RetrospectiveRuleActionEntity, int> repository,
        IRepository<EvidenceTypeMasterEntity, int> evidenceTypeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _evidenceTypeRepository = evidenceTypeRepository;
    }

    /// <summary>
    /// Display-label overrides for evidence codes whose natural "{EvidenceName} date" phrasing
    /// doesn't read well (e.g. "Construction Year date"). Falls back to "{EvidenceName} date" for
    /// any evidence type not listed here, so newly added evidence types still get a sane label.
    /// </summary>
    private static readonly Dictionary<string, string> _useDateLabelOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CHANGE_DETECTION"] = "Change detection date",
        ["CONSTRUCTION_YEAR"] = "Construction date/year",
    };

    public async Task<List<RetrospectiveRuleActionUseDateOptionDto>> GetUseDateOptionsAsync(CancellationToken cancellationToken = default)
    {
        var evidenceTypes = await _evidenceTypeRepository.GetQueryable()
            .Where(e => e.IsActive)
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync(cancellationToken);

        var options = evidenceTypes
            .Select(e => new RetrospectiveRuleActionUseDateOptionDto
            {
                EvidenceTypeId = e.Id,
                Label = _useDateLabelOverrides.TryGetValue(e.EvidenceCode, out var label) ? label : $"{e.EvidenceName} date",
                IsCutoffDate = false
            })
            .ToList();

        options.Add(new RetrospectiveRuleActionUseDateOptionDto
        {
            EvidenceTypeId = null,
            Label = "Cutoff date",
            IsCutoffDate = true
        });

        return options;
    }
}
