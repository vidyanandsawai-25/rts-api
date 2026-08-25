using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class GrievanceCategoryService : BaseCommonCrudService<GrievanceCategoryEntity, GrievanceCategoryDto, CreateGrievanceCategoryDto, UpdateGrievanceCategoryDto, GrievanceCategoryQueryParameters, int>, IGrievanceCategoryService
    {
        public GrievanceCategoryService(IRepository<GrievanceCategoryEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }

        protected override async Task<ValidationResult> ValidateForCreateAsync(
            GrievanceCategoryEntity entity, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(entity.CategoryCode))
            {
                var codeUpper = entity.CategoryCode.Trim().ToUpper();
                var queryable = _repository.GetQueryable();
                if (queryable != null)
                {
                    bool exists = queryable.Provider is IAsyncQueryProvider
                        ? await queryable.AsNoTracking().AnyAsync(x => x.CategoryCode != null && x.CategoryCode.Trim().ToUpper() == codeUpper, cancellationToken)
                        : queryable.Any(x => x.CategoryCode != null && x.CategoryCode.Trim().ToUpper() == codeUpper);

                    if (exists)
                    {
                        return ValidationResult.Failure(nameof(entity.CategoryCode), "Grievance category code must be unique.");
                    }
                }
            }

            return ValidationResult.Success();
        }

        protected override async Task<ValidationResult> ValidateForDeactivationAsync(
            int id, GrievanceCategoryEntity currentEntity, GrievanceCategoryEntity updatedEntity, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(updatedEntity.CategoryCode))
            {
                var codeUpper = updatedEntity.CategoryCode.Trim().ToUpper();
                var currentCodeUpper = currentEntity?.CategoryCode?.Trim()?.ToUpper();

                // Skip duplicate check if CategoryCode was not modified during update
                if (string.Equals(codeUpper, currentCodeUpper, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Success();
                }

                var queryable = _repository.GetQueryable();
                if (queryable != null)
                {
                    bool exists = queryable.Provider is IAsyncQueryProvider
                        ? await queryable.AsNoTracking().AnyAsync(x => x.Id != id && x.CategoryCode != null && x.CategoryCode.Trim().ToUpper() == codeUpper, cancellationToken)
                        : queryable.Any(x => x.Id != id && x.CategoryCode != null && x.CategoryCode.Trim().ToUpper() == codeUpper);

                    if (exists)
                    {
                        return ValidationResult.Failure(nameof(updatedEntity.CategoryCode), "Grievance category code must be unique.");
                    }
                }
            }

            return ValidationResult.Success();
        }
    }
}
