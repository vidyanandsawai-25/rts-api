using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGSTService :
    ICommonCrudService<GSTMasterEntity, GSTDto, CreateGSTDto, UpdateGSTDto, GSTQueryParameters, int>
{
}
