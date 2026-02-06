using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRateSectionDetailsService : ICommonCrudService<RateSectionDetailsEntity, RateSectionDetailsDto, CreateRateSectionDetailsDto, UpdateRateSectionDetailsDto, RateSectionDetailsQueryParameters, int>
{

}

