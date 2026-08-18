using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IRetrospectiveRuleActionService : ICommonCrudService<RetrospectiveRuleActionEntity, RetrospectiveRuleActionDto, CreateRetrospectiveRuleActionDto, UpdateRetrospectiveRuleActionDto, RetrospectiveRuleActionQueryParameters, int>
{
    /// <summary>
    /// Dropdown options for the "Use date" field: every active evidence type plus a synthetic
    /// "Cutoff date" entry. DB-driven (unlike TaxStartMode/ComparatorCode), so it stays in sync
    /// automatically as evidence types are added/renamed via EvidenceTypeMaster's own CRUD API.
    /// </summary>
    Task<List<RetrospectiveRuleActionUseDateOptionDto>> GetUseDateOptionsAsync(CancellationToken cancellationToken = default);
}
