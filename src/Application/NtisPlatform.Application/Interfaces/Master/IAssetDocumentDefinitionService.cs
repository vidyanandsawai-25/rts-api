using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetDocumentDefinitionService
    : ICommonCrudService<
        AssetDocumentDefinitionEntity,
        AssetDocumentDefinitionDto,
        CreateAssetDocumentDefinitionDto,
        UpdateAssetDocumentDefinitionDto,
        AssetDocumentDefinitionQueryParameters,
        int>
{
}
