using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveCalculationEvidenceService : BaseCommonCrudService<RetrospectiveCalculationEvidenceEntity, RetrospectiveCalculationEvidenceDto, CreateRetrospectiveCalculationEvidenceDto, UpdateRetrospectiveCalculationEvidenceDto, RetrospectiveCalculationEvidenceQueryParameters, long>, IRetrospectiveCalculationEvidenceService
{
    public RetrospectiveCalculationEvidenceService(
        IRepository<RetrospectiveCalculationEvidenceEntity, long> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
