using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Application.Services
{
    public class RetentionYearWiseService : BaseCommonCrudService<RetentionYearWiseEntity, RetentionYearWiseDto, CreateRetentionYearWiseDto, UpdateRetentionYearWiseDto, RetentionYearWiseQueryParameters, int>, IRetentionYearWiseService
    {
        public RetentionYearWiseService(IRepository<RetentionYearWiseEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }

        protected override async Task<Models.ValidationResult> ValidateForCreateAsync(RetentionYearWiseEntity entity, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var range in existing)
            {
                if ((entity.FromYear >= range.FromYear && entity.FromYear <= range.ToYear) ||
                    (entity.ToYear >= range.FromYear && entity.ToYear <= range.ToYear) ||
                    (range.FromYear >= entity.FromYear && range.FromYear <= entity.ToYear) ||
                    (range.ToYear >= entity.FromYear && range.ToYear <= entity.ToYear))
                {
                    return Models.ValidationResult.Failure($"Year range {entity.FromYear}-{entity.ToYear} overlaps with existing range {range.FromYear}-{range.ToYear}.");
                }
            }
            return Models.ValidationResult.Success();
        }

        protected override async Task<Models.ValidationResult> ValidateForDeactivationAsync(int id, RetentionYearWiseEntity currentEntity, RetentionYearWiseEntity updatedEntity, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetQueryable()
                .Where(x => x.Id != id && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var range in existing)
            {
                if ((updatedEntity.FromYear >= range.FromYear && updatedEntity.FromYear <= range.ToYear) ||
                    (updatedEntity.ToYear >= range.FromYear && updatedEntity.ToYear <= range.ToYear) ||
                    (range.FromYear >= updatedEntity.FromYear && range.FromYear <= updatedEntity.ToYear) ||
                    (range.ToYear >= updatedEntity.FromYear && range.ToYear <= updatedEntity.ToYear))
                {
                    return Models.ValidationResult.Failure($"Year range {updatedEntity.FromYear}-{updatedEntity.ToYear} overlaps with existing range {range.FromYear}-{range.ToYear}.");
                }
            }
            return Models.ValidationResult.Success();
        }
    }
}
