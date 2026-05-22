using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IOwningDepartmentService : ICommonCrudService<
        OwningDepartmentEntity,
        OwningDepartmentDto,
        CreateOwningDepartmentDto,
        UpdateOwningDepartmentDto,
        OwningDepartmentQueryParameters,
        int>
    {
    }
}
