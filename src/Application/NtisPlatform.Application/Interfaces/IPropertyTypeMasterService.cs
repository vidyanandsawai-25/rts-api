using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyTypeMasterService : ICommonCrudService<PropertyTypeMasterEntity, PropertyTypeMasterDto, CreatePropertyTypeMasterDto, UpdatePropertyTypeMasterDto, PropertyTypeMasterQueryParameters, int>
{
    /// <summary>
    /// Validates that no Property records reference this PropertyType, then permanently deletes it
    /// via the hard-delete cleanup service. Throws ValidationException (with the linked property
    /// count) if references exist. Returns false if the PropertyType was not found.
    /// </summary>
    Task<bool> ForceDeleteAsync(int id, CancellationToken cancellationToken = default);
}
