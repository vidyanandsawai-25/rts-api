using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IBulkUpdateFieldConfigService : ICommonCrudService<BulkUpdateFieldConfigEntity, BulkUpdateFieldConfigDto, CreateBulkUpdateFieldConfigDto, UpdateBulkUpdateFieldConfigDto, BulkUpdateFieldConfigQueryParameters, int>
{
}
