using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WardService : BaseCommonCrudService<WardEntity, WardDto, CreateWardDto, UpdateWardDto, WardQueryParameters, int>, IWardService
{
    public WardService(
        IRepository<WardEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    public async Task<RangeResult<WardDto>> CreateFromRangeAsync(RangeCreateRequest<CreateWardDto> request, CancellationToken cancellationToken = default)
    {
        // Internal transformer logic as previously in the controller
        Func<CreateWardDto, string, int, CreateWardDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateWardDto
            {
                WardNo = rangeValue,
                ZoneId = template.ZoneId,
                Description = string.IsNullOrEmpty(template.Description) ? $"Ward {rangeValue}" : template.Description.Replace("{value}", rangeValue),
                SequenceNo = sequenceNo,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };

        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }
}

