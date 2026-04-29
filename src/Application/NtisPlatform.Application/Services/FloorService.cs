using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorService : BaseCommonCrudService<FloorEntity, FloorDto, CreateFloorDto, UpdateFloorDto, FloorQueryParameters, int>, IFloorService
{
    public FloorService(
        IRepository<FloorEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
    public async Task<RangeResult<FloorDto>> CreateFromRangeAsync(RangeCreateRequest<CreateFloorDto> request, CancellationToken cancellationToken = default)
    {
        // Internal transformer logic as previously in the controller
        Func<CreateFloorDto, string, int, CreateFloorDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateFloorDto
            {
                FloorCode = rangeValue,
                MaxFloorNo = template.MaxFloorNo,
                Description = string.IsNullOrEmpty(template.Description) ? $"{rangeValue} Floor" : template.Description.Replace("{value}", rangeValue),
                SequenceNo = sequenceNo,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };
        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }
}
