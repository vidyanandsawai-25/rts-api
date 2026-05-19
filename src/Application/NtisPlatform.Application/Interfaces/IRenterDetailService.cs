using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRenterDetailService : ICommonCrudService<RenterDetailEntity, RenterDetailDto, CreateRenterDetailsDto, UpdateRenterDetailsDto, PropertyDetailsQueryParameters, int>
{
 
    Task CreateRangeAsync(int propertyDetailsId, IEnumerable<CreateRenterDetailsDto> dtos, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(int propertyDetailsId, IEnumerable<UpdateRenterDetailsDto> dtos, CancellationToken cancellationToken = default);
    Task DeleteByPropertyIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default);


}