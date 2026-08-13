using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IDynamicTaxRuleService
    : ICommonCrudService<
        DynamicTaxRuleEntity,
        DynamicTaxRuleDto,
        CreateDynamicTaxRuleDto,
        UpdateDynamicTaxRuleDto,
        DynamicTaxRuleQueryParameters,
        int>
{
}
