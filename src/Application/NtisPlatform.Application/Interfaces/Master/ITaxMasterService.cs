using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface ITaxMasterService
    : ICommonCrudService<
        TaxMasterEntity,
        TaxMasterDto,
        CreateTaxMasterDto,
        UpdateTaxMasterDto,
        TaxMasterQueryParameters,
        int>
{
}
