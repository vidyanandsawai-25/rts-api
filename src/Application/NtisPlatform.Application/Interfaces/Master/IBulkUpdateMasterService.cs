using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IBulkUpdateMasterService : ICommonCrudService<BulkUpdateMasterEntity, BulkUpdateMasterDto, CreateBulkUpdateMasterDto, UpdateBulkUpdateMasterDto, BulkUpdateMasterQueryParameters, int>
{
}
