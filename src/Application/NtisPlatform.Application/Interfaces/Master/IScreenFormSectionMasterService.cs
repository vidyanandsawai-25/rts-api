using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IScreenFormSectionMasterService
    : ICommonCrudService<ScreenFormSectionMasterEntity, ScreenFormSectionMasterDto,
                         CreateScreenFormSectionMasterDto, UpdateScreenFormSectionMasterDto,
                         ScreenFormSectionMasterQueryParameters, int>
{
}