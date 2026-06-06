using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface ISubZoneDetailsForCVService : ICommonCrudService<SubZoneDetailsForCVEntity, SubZoneDetailsForCVDto, CreateSubZoneDetailsForCVDto, UpdateSubZoneDetailsForCVDto, SubZoneDetailsForCVQueryParameters, int>
{
}
