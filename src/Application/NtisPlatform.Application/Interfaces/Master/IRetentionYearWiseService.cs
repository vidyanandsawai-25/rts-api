using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces
{
    public interface IRetentionYearWiseService : ICommonCrudService<RetentionYearWiseEntity, RetentionYearWiseDto, CreateRetentionYearWiseDto, UpdateRetentionYearWiseDto, RetentionYearWiseQueryParameters, int>
    {
    }
}
