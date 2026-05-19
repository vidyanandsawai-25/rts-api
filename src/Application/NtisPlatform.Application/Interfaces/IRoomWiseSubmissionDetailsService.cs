using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRoomWiseSubmissionDetailsService: ICommonCrudService<RoomWiseSubmissionDetailsEntity, RoomWiseSubmissionDetailsDto, CreateRoomWiseSubmissionDetailsDto, UpdateRoomWiseSubmissionDetailsDto, PropertyDetailsQueryParameters, int>
{
    Task CreateRangeAsync(int propertyDetailsId, IEnumerable<CreateRoomWiseSubmissionDetailsDto> dtos, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(int propertyDetailsId, IEnumerable<UpdateRoomWiseSubmissionDetailsDto> dtos, CancellationToken cancellationToken = default);
    Task DeleteByPropertyIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default);

}

