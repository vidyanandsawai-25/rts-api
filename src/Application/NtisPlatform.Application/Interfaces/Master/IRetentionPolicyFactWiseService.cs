using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;


namespace NtisPlatform.Application.Interfaces
{
    public interface IRetentionFactWiseService : ICommonCrudService<RetentionFactWiseEntity, RetentionFactWiseDto, CreateRetentionFactWiseDto, UpdateRetentionFactWiseDto, RetentionFactWiseQueryParameters, int>
    {
    }
}
