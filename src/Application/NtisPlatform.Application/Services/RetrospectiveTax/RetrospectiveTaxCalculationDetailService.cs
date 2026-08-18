using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveTaxCalculationDetailService : BaseCommonCrudService<RetrospectiveTaxCalculationDetailEntity, RetrospectiveTaxCalculationDetailDto, CreateRetrospectiveTaxCalculationDetailDto, UpdateRetrospectiveTaxCalculationDetailDto, RetrospectiveTaxCalculationDetailQueryParameters, long>, IRetrospectiveTaxCalculationDetailService
{
    public RetrospectiveTaxCalculationDetailService(
        IRepository<RetrospectiveTaxCalculationDetailEntity, long> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
