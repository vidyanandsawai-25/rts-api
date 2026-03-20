using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IZoneService : ICommonCrudService<ZoneEntity, ZoneDto, CreateZoneDto, UpdateZoneDto, ZoneQueryParameters, int>
{
}

