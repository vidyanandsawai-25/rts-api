using NtisPlatform.Application.DTOs.Master.BankMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IBankMasterService : ICommonCrudService<BankMasterEntity, BankMasterDTO, CreateBankMasterDto, UpdateBankMasterDto, BankQueryParameters, int>
    {
    }
}
