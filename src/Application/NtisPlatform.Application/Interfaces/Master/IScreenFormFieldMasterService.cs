using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IScreenFormFieldMasterService
    : ICommonCrudService<ScreenFormFieldMasterEntity, ScreenFormFieldMasterDto,
                         CreateScreenFormFieldMasterDto, UpdateScreenFormFieldMasterDto,
                         ScreenFormFieldMasterQueryParameters, int>
{
}