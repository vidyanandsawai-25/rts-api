using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IEvidenceTypeMasterService : ICommonCrudService<EvidenceTypeMasterEntity, EvidenceTypeMasterDto, CreateEvidenceTypeMasterDto, UpdateEvidenceTypeMasterDto, EvidenceTypeMasterQueryParameters, int>
{
}
