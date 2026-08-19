using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IEvidenceTypeMasterService : ICommonCrudService<EvidenceTypeMasterEntity, EvidenceTypeMasterDto, CreateEvidenceTypeMasterDto, UpdateEvidenceTypeMasterDto, EvidenceTypeMasterQueryParameters, int>
{
    /// <summary>
    /// Narrower overload the controller's no-transformer Range endpoint dynamically dispatches
    /// to (see CrudControllerExtensions.ExecuteCreateFromRange). Without this, the dynamic call
    /// finds no matching method on the base 3-arg CreateFromRangeAsync and throws at runtime.
    /// </summary>
    Task<RangeResult<EvidenceTypeMasterDto>> CreateFromRangeAsync(RangeCreateRequest<CreateEvidenceTypeMasterDto> request, CancellationToken cancellationToken = default);
}
