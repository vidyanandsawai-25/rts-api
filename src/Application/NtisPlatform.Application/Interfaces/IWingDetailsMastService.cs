using NtisPlatform.Application.DTOs.WingDetailsMast;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for WingDetailsMast CRUD operations
/// </summary>
public interface IWingDetailsMastService : ICommonCrudService<WingDetailsMastEntity, WingDetailsMastDto, CreateWingDetailsMastDto, UpdateWingDetailsMastDto, WingDetailsMastQueryParameters, int>
{
    Task<WingDetailsMastResponseDto> GetAllWithOldDetailsAsync(WingDetailsMastQueryParameters queryParameters, CancellationToken cancellationToken = default);

}
