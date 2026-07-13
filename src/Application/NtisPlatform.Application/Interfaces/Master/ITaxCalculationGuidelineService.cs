using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface ITaxCalculationGuidelineService
    : ICommonCrudService<TaxCalculationGuidelineEntity, TaxCalculationGuidelineDto, CreateTaxCalculationGuidelineDto, UpdateTaxCalculationGuidelineDto, TaxCalculationGuidelineQueryParameters, int>
{
}
