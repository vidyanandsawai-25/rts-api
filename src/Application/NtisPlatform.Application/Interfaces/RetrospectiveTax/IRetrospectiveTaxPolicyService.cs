using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IRetrospectiveTaxPolicyService : ICommonCrudService<RetrospectiveTaxPolicyEntity, RetrospectiveTaxPolicyDto, CreateRetrospectiveTaxPolicyDto, UpdateRetrospectiveTaxPolicyDto, RetrospectiveTaxPolicyQueryParameters, int>
{
    /// <summary>
    /// Narrower overload the controller's no-transformer Range endpoint dynamically dispatches
    /// to (see CrudControllerExtensions.ExecuteCreateFromRange). Without this, the dynamic call
    /// finds no matching method on the base 3-arg CreateFromRangeAsync and throws at runtime.
    /// </summary>
    Task<RangeResult<RetrospectiveTaxPolicyDto>> CreateFromRangeAsync(RangeCreateRequest<CreateRetrospectiveTaxPolicyDto> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// "Save Taxation" button on the "Taxation Rate &amp; Percentage" screen: upserts the single
    /// active policy row for the current ULB (there's only ever one, per
    /// UX_RetrospectiveTaxPolicy_OneActive) — updates it if one already exists, creates it
    /// otherwise. The caller never needs to know or pass an Id.
    /// </summary>
    Task<RetrospectiveTaxPolicyDto> SaveAsync(SaveRetrospectiveTaxPolicyDto request, CancellationToken cancellationToken = default);
}
