using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRateSectionService : ICommonCrudService<RateSectionEntity, RateSectionDto, CreateRateSectionDto, UpdateRateSectionDto, RateSectionQueryParameters, int>
{
}
