using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPropertyMapMasterService : ICommonCrudService<PropertyMapMasterEntity, PropertyMapMasterDtos, CreatePropertyMapMasterDto, UpdatePropertyMapMasterDto, PropertyMapQueryParameters, int>
{
    /// <summary>Returns a paged list of mapped old→new property pairs.</summary>
    Task<PagedResult<PropertyMapDetailReturnDto>> GetMappedPropertiesAsync(PropertyMapDetailQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>Returns a paged list of mapped new properties (New Survey details) based on Old PropertyId.</summary>
    Task<PagedResult<NewSurveyPropertyDto>> GetMappedNewPropertiesAsync(MappedNewPropertyQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>Returns a paged list of mapped old properties (Old Survey details) merged to a New PropertyId.</summary>
    Task<PagedResult<OldSurveyPropertyDto>> GetMappedOldPropertiesAsync(MappedOldPropertyQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>Returns a paged list of mapped old→new property pairs filtered society-wise or wing-wise.</summary>
    Task<PagedResult<PropertyMapSocietyReturnDto>> GetMappedPropertiesSocietyWiseAsync(PropertyMapSocietyQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches across up to 10 fields and returns:
    /// - Mapped property pairs (old + new) with match % and MappingDecision
    /// - Old property candidates (not-yet-mapped suggestions)
    /// - New property candidates (not-yet-mapped suggestions)
    /// </summary>
    Task<PropertyMapSearchResultDto> SearchPropertyMappingsAsync(PropertyMapDetailQueryParameters queryParams, CancellationToken cancellationToken = default);
}
