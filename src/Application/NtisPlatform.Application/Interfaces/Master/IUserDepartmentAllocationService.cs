using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Master.UserMaster;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IUserDepartmentAllocationService
{
    Task<IEnumerable<UserDepartmentDetailsDto>> GetMyAllocatedDepartmentsAsync(int userId, CancellationToken cancellationToken = default);
}
