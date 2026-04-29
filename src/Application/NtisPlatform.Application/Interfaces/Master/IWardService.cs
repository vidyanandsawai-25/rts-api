using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using NtisPlatform.Application.DTOs.Range;

namespace NtisPlatform.Application.Interfaces;

public interface IWardService : ICommonCrudService<WardEntity, WardDto, CreateWardDto, UpdateWardDto, WardQueryParameters, int>
{
    Task<RangeResult<WardDto>> CreateFromRangeAsync(RangeCreateRequest<CreateWardDto> request, CancellationToken cancellationToken = default);
}