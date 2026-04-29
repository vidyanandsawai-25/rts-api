using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WardService : BaseCommonCrudService<WardEntity, WardDto, CreateWardDto, UpdateWardDto, WardQueryParameters, int>, IWardService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WardService(
        IRepository<WardEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WardEntity currentEntity,
        WardEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<WardEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WardEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WardEntity>(id, cancellationToken);
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

