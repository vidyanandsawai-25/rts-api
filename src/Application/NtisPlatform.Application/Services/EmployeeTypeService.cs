using AutoMapper;
using NtisPlatform.Application.DTOs.Master.EmployeeType;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class EmployeeTypeService : BaseCommonCrudService<EmployeeTypeEntity, EmployeeTypeDto, CreateEmployeeTypeDto, UpdateEmployeeTypeDto, UserEmployeeTypeQueryParameterDto, int> ,IEmployeeType
    {
        public EmployeeTypeService(IRepository<EmployeeTypeEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
