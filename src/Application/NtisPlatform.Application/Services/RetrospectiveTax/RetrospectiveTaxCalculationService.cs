using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveTaxCalculationService : BaseCommonCrudService<RetrospectiveTaxCalculationEntity, RetrospectiveTaxCalculationDto, CreateRetrospectiveTaxCalculationDto, UpdateRetrospectiveTaxCalculationDto, RetrospectiveTaxCalculationQueryParameters, int>, IRetrospectiveTaxCalculationService
{
    public RetrospectiveTaxCalculationService(
        IRepository<RetrospectiveTaxCalculationEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
