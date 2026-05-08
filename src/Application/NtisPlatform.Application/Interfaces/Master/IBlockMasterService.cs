using NtisPlatform.Application.DTOs.Master.BlockMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IBlockMasterService : ICommonCrudService<BlockMasterEntity, BlockMasterDtos, CreateBlockMasterDto, UpdateBlockMasterDto, BlockQueryParameters, int>
{
}