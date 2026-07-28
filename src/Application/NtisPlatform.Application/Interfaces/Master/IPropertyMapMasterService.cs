using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPropertyMapMasterService : ICommonCrudService<PropertyMapMasterEntity, PropertyMapMasterDtos, CreatePropertyMapMasterDto, UpdatePropertyMapMasterDto, PropertyMapQueryParameters, int>
{
    /// <summary>Returns a paged list of mapped old→new property pairs.</summary>
    Task<PagedResult<PropertyMapDetailReturnDto>> GetMappedPropertiesAsync(PropertyMapDetailQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches across up to 10 fields and returns:
    /// - Mapped property pairs (old + new) with match % and MappingDecision
    /// - Old property candidates (not-yet-mapped suggestions)
    /// - New property candidates (not-yet-mapped suggestions)
    /// </summary>
    Task<PropertyMapSearchResultDto> SearchPropertyMappingsAsync(PropertyMapDetailQueryParameters queryParams, CancellationToken cancellationToken = default);
}
