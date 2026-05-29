
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRenterMastService : ICommonCrudService<RenterMastEntity, RenterMastDto, CreateRenterMastDto, UpdateRenterMastDto, PropertyDetailsQueryParameters, int>
{
    Task CreateRangeAsync(int propertyDetailsId, IEnumerable<CreateRenterMastDto> dtos, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(int propertyDetailsId, IEnumerable<UpdateRenterMastDto> dtos, bool isRenter = false, CancellationToken cancellationToken = default);
    Task DeleteByPropertyIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default);
}