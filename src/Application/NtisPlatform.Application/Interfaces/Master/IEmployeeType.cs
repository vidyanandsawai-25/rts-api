using NtisPlatform.Application.DTOs.Master.EmployeeType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IEmployeeType : ICommonCrudService<EmployeeTypeEntity, EmployeeTypeDto, CreateEmployeeTypeDto, UpdateEmployeeTypeDto, UserEmployeeTypeQueryParameterDto, int>
    {
    }
}
