using AutoMapper;
using NtisPlatform.Application.DTOs.Master.BankMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class BankMasterService : BaseCommonCrudService<BankMasterEntity, BankMasterDTO, CreateBankMasterDto, UpdateBankMasterDto, BankQueryParameters, int>, IBankMasterService
    {
        public BankMasterService(
            IRepository<BankMasterEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
