using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>
/// Service interface for sub-unit details (AMS.SubUnitsDetails) CRUD operations.
/// </summary>
public interface ISubUnitsDetailsService : ICommonCrudService<SubUnitsDetailsEntity, SubUnitsDetailsDto, CreateSubUnitsDetailsDto, UpdateSubUnitsDetailsDto, SubUnitsDetailsQueryParameters, int>
{
    /// <summary>
    /// Gets all floor details for a specific asset with summary totals.
    /// </summary>
    Task<SubUnitsDetailsSummaryDto> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers rooms directly against a building floor by creating necessary property groups and room-wise submission details.
    /// </summary>
    Task<bool> CreateDirectRoomsAsync(DirectRoomRegistrationDto dto, int currentUserId, CancellationToken cancellationToken = default);
}
