using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetTypeService
    : BaseCommonCrudService<
        AssetTypeEntity,
        AssetTypeDto,
        CreateAssetTypeDto,
        UpdateAssetTypeDto,
        AssetTypeQueryParameters,
        int>,
      IAssetTypeService
{
    public AssetTypeService(
        IRepository<AssetTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}