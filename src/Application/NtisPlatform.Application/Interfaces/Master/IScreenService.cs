using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IScreenService
    : ICommonCrudService<ScreenEntity, ScreenDto,
                         CreateScreenDto, UpdateScreenDto,
                         ScreenQueryParameters, int>
{
}