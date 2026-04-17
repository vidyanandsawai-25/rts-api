using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for RateSectionDetails entity operations.
/// Permanent deletion is handled through IHardDeleteCleanupService for centralized policy enforcement.
/// </summary>
public interface IRateSectionDetailsService : 
    ICommonCrudService<RateSectionDetailsEntity, RateSectionDetailsDto, CreateRateSectionDetailsDto, UpdateRateSectionDetailsDto, RateSectionDetailsQueryParameters, int>
{

}

