using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyChangeCategory;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyChangeCategoryService : BaseCommonCrudService<PropertyMapDetailEntity, PropertyChangeCategoryDto, CreatePropertyChangeCategoryDto, UpdatePropertyChangeCategoryDto, PropertyChangeCategoryQueryParameters, int>, IPropertyChangeCategoryService
{
    private readonly ILogger<PropertyChangeCategoryService> _logger;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;

    public PropertyChangeCategoryService(
        IRepository<PropertyMapDetailEntity, int> repository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyChangeCategoryService> logger,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _logger = logger;
        _repository = propertyRepository;
        _categoryRepository = categoryRepository;
        _societyRepository = societyRepository; 
    }

    public override async Task<PropertyChangeCategoryDto?> UpdateAsync(int id, UpdatePropertyChangeCategoryDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Starting property category change. PropertyId: {PropertyId}, NewCategoryId: {CategoryId}, UpdatedBy: {UpdatedBy}", dto.PropertyId, dto.CategoryId, dto.UpdatedBy);

            var propertyMast = await (
            from property in _repository.GetQueryable().AsNoTracking()
            join category in _categoryRepository.GetQueryable().AsNoTracking()
                on property.CategoryId equals category.Id
            where property.Id == dto.PropertyId
                  && property.IsActive
                  && !property.MarkedForDeletion
                  && category.IsActive
            select new
            {
                property.Id,
                property.WardId,
                property.PropertyNo,
                property.CategoryId,
                CategoryName = category.PropertyCategoryName
            })
            .FirstOrDefaultAsync(cancellationToken);

            if (propertyMast == null)
            {
                throw new ValidationException("Property", "Property not found", OperationType.Update);
            }

            if (dto.CategoryId == propertyMast.CategoryId)
            {
                throw new ValidationException("category", $"The current category is already '{propertyMast.CategoryName}'. Please select a different category.", OperationType.Update);
            }

            // Fetch new category name once for validation + success message
            var newCategoryName = await _categoryRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == dto.CategoryId && x.IsActive)
                .Select(x => x.PropertyCategoryName)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(newCategoryName))
            {
                throw new ValidationException("Category", "Selected category not found.", OperationType.Update);
            }

            var dateTimeNow = DateTime.Now;

            var isApartmentCategory = string.Equals(propertyMast.CategoryName, "Apartment", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(propertyMast.CategoryName, "Multi Commercial Apartment", StringComparison.OrdinalIgnoreCase);

            if (isApartmentCategory)
            {
                var hasMultipleProperties = await _repository.GetQueryable().AsNoTracking()
                    .Where(property =>
                        property.Id != dto.PropertyId &&
                        property.WardId == propertyMast.WardId &&
                        property.PropertyNo == propertyMast.PropertyNo &&
                        property.IsActive && !property.MarkedForDeletion)
                    .AnyAsync(cancellationToken);

                if (hasMultipleProperties)
                {
                    throw new ValidationException("category", "The category cannot be changed because flat, shop, or wing properties already exist for this building. Delete those properties and try again.", OperationType.Update);
                }

                var UpdateCount = await _societyRepository.GetQueryable()
                        .Where(x => x.PropertyId == dto.PropertyId && x.IsActive && !x.MarkedForDeletion)
                        .ExecuteUpdateAsync(
                            set => set.SetProperty(x => x.IsActive, false)
                                       .SetProperty(x => x.MarkedForDeletion, true)
                                       .SetProperty(x => x.MarkedForDeletionDate, dateTimeNow)
                                       .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                                       .SetProperty(x => x.UpdatedDate, dateTimeNow),
                            cancellationToken);

                var affectedRow = await _repository.GetQueryable()
               .Where(x => x.Id == dto.PropertyId && x.IsActive && !x.MarkedForDeletion)
               .ExecuteUpdateAsync(
                   set => set.SetProperty(x => x.CategoryId, dto.CategoryId)
                              .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                              .SetProperty(x => x.UpdatedDate, dateTimeNow),
                   cancellationToken);

                if (affectedRow == 0)
                {
                    throw new InvalidOperationException("The property category could not be updated.");
                }
            }
            else
            {
                var affectedRows = await _repository.GetQueryable()
                .Where(x => x.Id == dto.PropertyId
                         && x.IsActive
                         && !x.MarkedForDeletion)
                .ExecuteUpdateAsync(
                    set => set
                        .SetProperty(x => x.CategoryId, dto.CategoryId)
                        // Owner
                        .SetProperty(x => x.OwnerTitle, (string?)null)
                        .SetProperty(x => x.OwnerTitleEnglish, (string?)null)
                        .SetProperty(x => x.OwnerName, (string?)null)
                        .SetProperty(x => x.OwnerNameEnglish, (string?)null)
                        .SetProperty(x => x.MobileNo, (string?)null)
                        .SetProperty(x => x.MobileNoRemarkId, (int?)null)
                        .SetProperty(x => x.AlternateMobileNo, (string?)null)
                        // Occupier
                        .SetProperty(x => x.OccupierTitle, (string?)null)
                        .SetProperty(x => x.OccupierTitleEnglish, (string?)null)
                        .SetProperty(x => x.OccupierName, (string?)null)
                        .SetProperty(x => x.OccupierNameEnglish, (string?)null)
                        .SetProperty(x => x.OccupierMobileNo, (string?)null)
                        .SetProperty(x => x.OccupierMobileNoRemarkId, (int?)null)
                        // Flat / Shop
                        .SetProperty(x => x.FlatOrShopName, (string?)null)
                        .SetProperty(x => x.FlatOrShopNameEnglish, (string?)null)
                        .SetProperty(x => x.FlatOrShopNo, (string?)null)
                        .SetProperty(x => x.FlatOrShopNoEnglish, (string?)null)
                        // Audit
                        .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                        .SetProperty(x => x.UpdatedDate, dateTimeNow),
                    cancellationToken);

            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Property category changed successfully. PropertyId: {PropertyId}, OldCategory: {OldCategory}, NewCategory: {NewCategory}, UpdatedBy: {UpdatedBy}", dto.PropertyId, propertyMast.CategoryName, newCategoryName, dto.UpdatedBy);
            return new PropertyChangeCategoryDto
            {
                Success = true,
                Message = $"Property category changed successfully from '{propertyMast.CategoryName}' to '{newCategoryName}'."
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error while changing property category. PropertyId: {PropertyId}, CategoryId: {CategoryId}, UpdatedBy: {UpdatedBy}", dto.PropertyId, dto.CategoryId, dto.UpdatedBy);
            throw;
        }
    }
}
