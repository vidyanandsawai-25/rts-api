using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services
{
    public class RetentionFactWiseService : BaseCommonCrudService<RetentionFactWiseEntity, RetentionFactWiseDto, CreateRetentionFactWiseDto, UpdateRetentionFactWiseDto, RetentionFactWiseQueryParameters, int>, IRetentionFactWiseService
    {
        public RetentionFactWiseService(IRepository<RetentionFactWiseEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }

        protected override async Task<ValidationResult> ValidateForCreateAsync(RetentionFactWiseEntity entity, CancellationToken cancellationToken = default)
        {
            // Overlap check for active records
            var existing = await _repository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var range in existing)
            {
                if ((entity.FromFactor >= range.FromFactor && entity.FromFactor < range.ToFactor) ||
                    (entity.ToFactor > range.FromFactor && entity.ToFactor <= range.ToFactor) ||
                    (range.FromFactor >= entity.FromFactor && range.FromFactor < entity.ToFactor) ||
                    (range.ToFactor > entity.FromFactor && range.ToFactor <= entity.ToFactor))
                {
                    return Models.ValidationResult.Failure($"Factor range {entity.FromFactor}-{entity.ToFactor} overlaps with existing range {range.FromFactor}-{range.ToFactor}.");
                }
            }
            return Models.ValidationResult.Success();
        }

        protected override async Task<ValidationResult> ValidateForDeactivationAsync(int id, RetentionFactWiseEntity currentEntity, RetentionFactWiseEntity updatedEntity, CancellationToken cancellationToken = default)
        {
            // Overlap check for active records (on update)
            var existing = await _repository.GetQueryable()
                .Where(x => x.Id != id && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var range in existing)
            {
                if ((updatedEntity.FromFactor >= range.FromFactor && updatedEntity.FromFactor < range.ToFactor) ||
                    (updatedEntity.ToFactor > range.FromFactor && updatedEntity.ToFactor <= range.ToFactor) ||
                    (range.FromFactor >= updatedEntity.FromFactor && range.FromFactor < updatedEntity.ToFactor) ||
                    (range.ToFactor > updatedEntity.FromFactor && range.ToFactor <= updatedEntity.ToFactor))
                {
                    return Models.ValidationResult.Failure($"Factor range {updatedEntity.FromFactor}-{updatedEntity.ToFactor} overlaps with existing range {range.FromFactor}-{range.ToFactor}.");
                }
            }
            return Models.ValidationResult.Success();
        }
    }
}
