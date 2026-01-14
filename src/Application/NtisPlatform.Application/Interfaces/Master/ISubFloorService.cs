using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ISubFloorService : ICommonCrudService<SubFloorEntity, SubFloorDto, CreateSubFloorDto, UpdateSubFloorDto, SubFloorQueryParameters, string>
{
}
