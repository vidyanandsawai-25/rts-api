using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IRTSFieldDefinitionService: ICommonCrudService<RTSFieldDefinitionEntity, RTSFieldDefinitionDto, CreateRTSFieldDefinitionDto, UpdateRTSFieldDefinitionDto,RTSFieldDefinitionQueryParameters,int>
{
}
