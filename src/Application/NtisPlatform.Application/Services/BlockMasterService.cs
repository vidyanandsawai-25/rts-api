using AutoMapper;
using NtisPlatform.Application.DTOs.Master.BlockMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class BlockMasterService : BaseCommonCrudService<BlockMasterEntity, BlockMasterDtos, CreateBlockMasterDto, UpdateBlockMasterDto, BlockQueryParameters, int>, IBlockMasterService
{
    public BlockMasterService(
        IRepository<BlockMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}