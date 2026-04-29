using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Application.Services
{
    public class AssessmentYearRangeCVService : BaseCommonCrudService<AssessmentYearRangeCVEntity, AssessmentYearRangeCVDto, CreateAssessmentYearRangeCVDto, UpdateAssessmentYearRangeCVDto, AssessmentYearRangeCVQueryParameters, int>, IAssessmentYearRangeCVService
    {
        private readonly IReferenceValidationService _referenceValidator;

        public AssessmentYearRangeCVService(
            IRepository<AssessmentYearRangeCVEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IReferenceValidationService referenceValidator)
            : base(repository, unitOfWork, mapper)
        {
            _referenceValidator = referenceValidator;
        }

        protected override async Task<ValidationResult> ValidateForDeactivationAsync(
            int id,
            AssessmentYearRangeCVEntity currentEntity,
            AssessmentYearRangeCVEntity updatedEntity,
            CancellationToken cancellationToken = default)
        {
            if (currentEntity.IsActive && !updatedEntity.IsActive)
            {
                var referenceResult = await _referenceValidator.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(id, cancellationToken);
                if (!referenceResult.IsValid)
                    return referenceResult;
            }
  
            // Year range overlap validation (on any update)
            var existingRanges = await _repository.GetQueryable()
                .Where(x => x.Id != id)
                .ToListAsync(cancellationToken);

            foreach (var range in existingRanges)
            {
                if ((updatedEntity.FromYear >= range.FromYear && updatedEntity.FromYear <= range.ToYear) ||
                    (updatedEntity.ToYear >= range.FromYear && updatedEntity.ToYear <= range.ToYear) ||
                    (range.FromYear >= updatedEntity.FromYear && range.FromYear <= updatedEntity.ToYear) ||
                    (range.ToYear >= updatedEntity.FromYear && range.ToYear <= updatedEntity.ToYear))
                {
                    return ValidationResult.Failure($"Year range {updatedEntity.FromYear}-{updatedEntity.ToYear} overlaps with existing range {range.FromYear}-{range.ToYear}.");
                }
            }
            return ValidationResult.Success();
        }

        protected override async Task<ValidationResult> ValidateForDeleteAsync(
            int id,
            AssessmentYearRangeCVEntity entity,
            CancellationToken cancellationToken = default)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(id, cancellationToken);
        }
        protected override async Task<ValidationResult> ValidateForCreateAsync(AssessmentYearRangeCVEntity entity, CancellationToken cancellationToken = default)
        {
            var existingRanges = await _repository.GetQueryable().ToListAsync(cancellationToken);
            foreach (var range in existingRanges)
            {
                if ((entity.FromYear >= range.FromYear && entity.FromYear <= range.ToYear) ||
                    (entity.ToYear >= range.FromYear && entity.ToYear <= range.ToYear) ||
                    (range.FromYear >= entity.FromYear && range.FromYear <= entity.ToYear) ||
                    (range.ToYear >= entity.FromYear && range.ToYear <= entity.ToYear))
                {
                    return ValidationResult.Failure($"Year range {entity.FromYear}-{entity.ToYear} overlaps with existing range {range.FromYear}-{range.ToYear}.");
                }
            }
            return ValidationResult.Success();
        }
    }
}
