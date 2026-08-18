using NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

/// <summary>
/// Read-only composite view over RetrospectiveRuleMaster + RetrospectiveRuleAction +
/// RetrospectivePenaltyRule + EvidenceTypeMaster + RetrospectiveTaxPolicy for the
/// "Corporation Rule Library" grid. Not a CRUD service — there's nothing to create/update here;
/// each underlying table already has its own CRUD API for the "Edit" action.
/// </summary>
public interface IRuleLibraryService
{
    Task<RuleLibraryDto> GetLibraryAsync(RuleLibraryQueryParameters queryParameters, CancellationToken cancellationToken = default);
}
