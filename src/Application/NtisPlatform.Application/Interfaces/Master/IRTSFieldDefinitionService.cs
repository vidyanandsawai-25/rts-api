using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IRTSFieldDefinitionService: ICommonCrudService<RTSFieldDefinitionEntity, RTSFieldDefinitionDto, CreateRTSFieldDefinitionDto, UpdateRTSFieldDefinitionDto,RTSFieldDefinitionQueryParameters,int>
{
    Task<PagedResult<RTSFieldDefinitionServiceCardDto>> GetServiceCardsAsync(
        RTSFieldDefinitionServiceCardQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RTSFieldDefinitionDto>> UpdateServiceFormAsync(
        int serviceId,
        UpdateRTSFieldDefinitionFormDto request,
        CancellationToken cancellationToken = default);
}
