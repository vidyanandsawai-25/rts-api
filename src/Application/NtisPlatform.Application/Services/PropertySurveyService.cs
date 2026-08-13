using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Application.Services;

public class PropertySurveyService : IPropertySurveyService
{
    private const string ImageMimeTypePrefix = "image/";

    private static readonly string[] SupportedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif",
        ".bmp",
        ".heic"
    ];

    private readonly IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleMasterRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingEntity, int> _wingMasterRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _propertyPhotoRepository;
    private readonly IRepository<SocietyWingDetailsEntity, int> _societyWingRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseRepository;

    public PropertySurveyService(
        IRepository<PropertyEntity, int> repository,
        IRepository<ModuleMasterEntity, int> moduleMasterRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingEntity, int> wingMasterRepository,
        IRepository<PropertyPhotoEntity, int> propertyPhotoRepository,
        IRepository<SocietyWingDetailsEntity, int> societyWingRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseRepository)
    {
        _repository = repository;
        _moduleMasterRepository = moduleMasterRepository;
        _wardRepository = wardRepository;
        _categoryRepository = categoryRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyOldRepository = propertyOldRepository;
        _societyRepository = societyRepository;
        _wingMasterRepository = wingMasterRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
        _societyWingRepository = societyWingRepository;
        _roomWiseRepository = roomWiseRepository;
    }

    public async Task<UserPropertyPageDto> SearchNewlyCreatedPropertiesAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var moduleExists = await _moduleMasterRepository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(module => module.Id == request.ModuleId && module.IsActive, cancellationToken);

        if (!moduleExists)
        {
            throw new ArgumentException(
                $"Invalid or inactive ModuleId: {request.ModuleId}");
        }

        return await SearchNewlyCreatedPropertiesCoreAsync(
            request,
            cancellationToken);
    }

    private async Task<UserPropertyPageDto> SearchNewlyCreatedPropertiesCoreAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var effectiveSearch = !string.IsNullOrWhiteSpace(request.SearchText)
            ? request.SearchText.Trim()
            : request.SearchTerm?.Trim();

        // Validate pagination and calculate skip early to avoid running queries with invalid values
        var skip = ValidateAndCalculateSkip(request.PageNumber, request.PageSize);

        var excludedAmenityPropertyTypeIds = _propertyTypeRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(propertyType =>
                propertyType.PartType == PropertyPartTypes.Amenity &&
                propertyType.Type == PropertyTypeCodes.Ratable)
            .Select(propertyType => (int?)propertyType.Id);

        var query =
            from property in _repository
                .GetQueryable()
                .AsNoTracking()

            join ward in _wardRepository
                    .GetQueryable()
                    .AsNoTracking()
                on property.WardId equals ward.Id
                into wardGroup

            from ward in wardGroup.DefaultIfEmpty()

            join category in _categoryRepository
                    .GetQueryable()
                    .AsNoTracking()
                on property.CategoryId equals category.Id
                into categoryGroup

            from category in categoryGroup.DefaultIfEmpty()

            join propertyType in _propertyTypeRepository
                    .GetQueryable()
                    .AsNoTracking()
                on property.PropertyTypeId equals propertyType.Id
                into propertyTypeGroup

            from propertyType in propertyTypeGroup.DefaultIfEmpty()

            join mapDetail in _propertyMapDetailRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(map =>
                        map.PropertyIdNew.HasValue &&
                        map.PropertyIdOld.HasValue &&
                        map.IsActive)
                on property.Id equals mapDetail.PropertyIdNew!.Value
                into mapDetailGroup

            from mapDetail in mapDetailGroup
                .OrderByDescending(map => map.IsCurrent)
                .ThenByDescending(map => map.UpdatedDate)
                .ThenByDescending(map => map.Id)
                .Take(1)
                .DefaultIfEmpty()

            join oldProperty in _propertyOldRepository
                    .GetQueryable()
                    .AsNoTracking()
                on (
                    mapDetail != null &&
                    mapDetail.PropertyIdOld.HasValue
                        ? mapDetail.PropertyIdOld.Value
                        : 0
                )
                equals oldProperty.Id
                into oldPropertyGroup

            from oldProperty in oldPropertyGroup.DefaultIfEmpty()

            where property.CreatedBy == request.UserId
                  && property.WardId == request.WardId
                  && property.IsActive
                  && !property.MarkedForDeletion
                  && !excludedAmenityPropertyTypeIds.Contains(
                      property.PropertyTypeId)

                  // Exclude properties whose PartitionNo is a WingNo.
                  && !_societyRepository
                      .GetQueryable()
                      .Any(society =>
                          society.PropertyId.HasValue &&
                          society.PropertyId.Value == property.Id &&
                          society.WingId.HasValue &&
                          society.IsActive &&
                          !society.MarkedForDeletion &&

                          _wingMasterRepository
                              .GetQueryable()
                              .Any(wing =>
                                  wing.Id == society.WingId.Value &&
                                  wing.IsActive &&
                                  property.PartitionNo != null &&
                                  wing.WingNo == property.PartitionNo))

            select new
            {
                Property = property,
                Ward = ward,
                Category = category,
                PropertyType = propertyType,
                OldProperty = oldProperty
            };

        if (!string.IsNullOrWhiteSpace(effectiveSearch))
        {
            query = query.Where(x =>
                (x.Property.PropertyNo != null &&
                 x.Property.PropertyNo.Contains(effectiveSearch)) ||

                (x.Property.PartitionNo != null &&
                 x.Property.PartitionNo.Contains(effectiveSearch)) ||

                (x.Category != null &&
                 x.Category.PropertyCategoryName != null &&
                 x.Category.PropertyCategoryName.Contains(effectiveSearch)) ||

                (x.PropertyType != null &&
                 x.PropertyType.PropertyDescription != null &&
                 x.PropertyType.PropertyDescription.Contains(effectiveSearch)) ||

                (x.Property.OwnerName != null &&
                 x.Property.OwnerName.Contains(effectiveSearch)) ||

                (x.Property.OccupierName != null &&
                 x.Property.OccupierName.Contains(effectiveSearch)) ||

                (x.Property.Address != null &&
                 x.Property.Address.Contains(effectiveSearch)) ||

                (x.Property.UPICId != null &&
                 x.Property.UPICId.Contains(effectiveSearch)) ||

                _societyRepository
                    .GetQueryable()
                    .Any(society =>
                        society.PropertyId.HasValue &&
                        society.PropertyId.Value == x.Property.Id &&
                        society.IsActive &&
                        !society.MarkedForDeletion &&
                        (
                            (society.SocietyName != null &&
                             society.SocietyName.Contains(effectiveSearch)) ||

                            (society.BuilderName != null &&
                             society.BuilderName.Contains(effectiveSearch)) ||

                            (society.SocietyAddress != null &&
                             society.SocietyAddress.Contains(effectiveSearch))
                        )));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = query
            .OrderByDescending(x => x.Property.PropertySeqNo.HasValue)
            .ThenByDescending(x => x.Property.PropertySeqNo)
            .ThenByDescending(x => x.Property.Id);

        /*
         * Finds the property having the highest PropertySeqNo
         * in the selected ward.
         *
         * Only this property receives CanDelete = true.
         *
         * Amenity and Wing properties are excluded.
         */
        var maxWardPropertyId = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(property =>
                property.WardId == request.WardId &&
                property.PropertySeqNo.HasValue &&
                property.IsActive &&
                !property.MarkedForDeletion &&
                !excludedAmenityPropertyTypeIds.Contains(
                    property.PropertyTypeId) &&

                !_societyRepository
                    .GetQueryable()
                    .Any(society =>
                        society.PropertyId.HasValue &&
                        society.PropertyId.Value == property.Id &&
                        society.WingId.HasValue &&
                        society.IsActive &&
                        !society.MarkedForDeletion &&

                        _wingMasterRepository
                            .GetQueryable()
                            .Any(wing =>
                                wing.Id == society.WingId.Value &&
                                wing.IsActive &&
                                property.PartitionNo != null &&
                                wing.WingNo == property.PartitionNo)))
            .OrderByDescending(property => property.PropertySeqNo)
            .ThenByDescending(property => property.Id)
            .Select(property => (int?)property.Id)
            .FirstOrDefaultAsync(cancellationToken);

        List<CreatedByUserPropertyResponseDto> data;

        if (request.PageSize == -1)
        {
            data = await orderedQuery
                .Select(x => new CreatedByUserPropertyResponseDto
            {
                Id = x.Property.Id,
                WardId = x.Property.WardId,
                WardNo = x.Ward != null ? x.Ward.WardNo : null,
                PropertyNo = x.Property.PropertyNo,
                PartitionNo = x.Property.PartitionNo,
                CategoryId = x.Property.CategoryId,
                CategoryName = x.Category != null ? x.Category.PropertyCategoryName : null,
                PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                OldAlv = x.OldProperty != null ? x.OldProperty.OldALV : null,
                OldRv = x.OldProperty != null ? x.OldProperty.OldRV : null,
                OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                OwnerName = x.Property.OwnerName,
                OccupierName = x.Property.OccupierName,
                Address = x.Property.Address,
                UpicId = x.Property.UPICId,
                CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value,
                })
                .ToListAsync(cancellationToken);
        }
        else
        {
            data = await orderedQuery
                .Skip(skip)
                .Take(request.PageSize)
                .Select(x => new CreatedByUserPropertyResponseDto
                {
                    Id = x.Property.Id,
                    WardId = x.Property.WardId,
                    WardNo = x.Ward != null ? x.Ward.WardNo : null,
                    PropertyNo = x.Property.PropertyNo,
                    PartitionNo = x.Property.PartitionNo,
                    CategoryId = x.Property.CategoryId,
                    CategoryName = x.Category != null ? x.Category.PropertyCategoryName : null,
                    PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                    OldAlv = x.OldProperty != null ? x.OldProperty.OldALV : null,
                    OldRv = x.OldProperty != null ? x.OldProperty.OldRV : null,
                    OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                    OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                    OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                    OwnerName = x.Property.OwnerName,
                    OccupierName = x.Property.OccupierName,
                    Address = x.Property.Address,
                    UpicId = x.Property.UPICId,
                    CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value,
                })
                .ToListAsync(cancellationToken);
        }

        await AttachPropertyDocumentsAsync(data, cancellationToken);
        await AttachSocietyDetailsAsync(data, cancellationToken);
        await AttachTotalAreaAsync(data, cancellationToken);
        int totalPages;
        int responsePageNumber = request.PageNumber;
        bool hasNext;

        if (request.PageSize == -1)
        {
            totalPages = totalCount == 0 ? 0 : 1;
            responsePageNumber = totalCount == 0 ? 1 : 1;
            hasNext = false;
        }
        else
        {
            totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)request.PageSize);

            hasNext = request.PageNumber < totalPages;
        }

        return new UserPropertyPageDto
        {
            Items = data,
            PageItemCount = data.Count,
            TotalCount = totalCount,
            PageNumber = responsePageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNext = hasNext
        };
    }

    private static int ValidateAndCalculateSkip(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentException("PageNumber must be greater than 0.");
        }

        if (pageSize == 0 || pageSize < -1)
        {
            throw new ArgumentException("PageSize must be greater than 0 or equal to -1 to fetch all records.");
        }

        if (pageSize == -1)
        {
            return 0;
        }

        try
        {
            return checked((pageNumber - 1) * pageSize);
        }
        catch (OverflowException)
        {
            throw new ArgumentException("The combination of PageNumber and PageSize is too large.");
        }
    }

    private async Task AttachPropertyDocumentsAsync(
        List<CreatedByUserPropertyResponseDto> properties,
        CancellationToken cancellationToken)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var propertyIds = properties
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var propertyDocuments = await
        (
            from photo in _propertyPhotoRepository
                .GetQueryable()
                .AsNoTracking()

            where propertyIds.Contains(photo.PropertyId)
                  && photo.IsActive
                  && !photo.MarkedForDeletion
                  && photo.DocumentBinding != null
                  && photo.DocumentBinding.Document != null
                  && photo.DocumentBinding.Document.IsActive
                  && ((photo.DocumentBinding.Document.MimeType != null && photo.DocumentBinding.Document.MimeType.StartsWith(ImageMimeTypePrefix)) ||
                      (photo.DocumentBinding.Document.FileExtension != null && SupportedImageExtensions.Contains(photo.DocumentBinding.Document.FileExtension.ToLower())))
            orderby
                photo.PropertyId,
                photo.IsLatest descending,
                photo.DisplayOrder descending,
                photo.Id descending

            select new PropertySearchDocumentDto
            {
                PropertyPhotoId = photo.Id,
                PropertyId = photo.PropertyId,
                PhotoTypeId = photo.PhotoTypeId,
                DocumentId = photo.DocumentBinding!.Document!.Id,
                DocumentGuid = photo.DocumentBinding!.Document!.DocumentGuid,
                OriginalFileName = photo.DocumentBinding!.Document!.OriginalFileName,
                FileName = photo.DocumentBinding!.Document!.FileName,
                MimeType = photo.DocumentBinding!.Document!.MimeType,
                FileExtension = photo.DocumentBinding!.Document!.FileExtension,
                FileSizeBytes = photo.DocumentBinding!.Document!.FileSizeBytes,
                DocumentBindingId = photo.DocumentBindingId,
                IsLatest = photo.IsLatest,
                DisplayOrder = photo.DisplayOrder,
                Remarks = photo.Remarks
            }
        )
        .ToListAsync(cancellationToken);

        var documentsByPropertyId = propertyDocuments
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        foreach (var property in properties)
        {
            property.Documents = documentsByPropertyId.TryGetValue(property.Id, out var documents)
                ? documents
                : new List<PropertySearchDocumentDto>();
        }
    }

    private async Task AttachSocietyDetailsAsync(
        List<CreatedByUserPropertyResponseDto> properties,
        CancellationToken cancellationToken)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var propertyIds = properties
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var societyDetails = await _societyRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(society =>
                society.PropertyId.HasValue &&
                propertyIds.Contains(society.PropertyId.Value) &&
                society.IsActive &&
                !society.MarkedForDeletion)
            .Select(society => new
            {
                society.Id,
                PropertyId = society.PropertyId!.Value,
                society.SocietyName,
                society.BuilderName,
                society.SocietyAddress
            })
            .ToListAsync(cancellationToken);

        var wingCounts = await _societyWingRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(wing =>
                wing.PropertyId.HasValue &&
                propertyIds.Contains(wing.PropertyId.Value) &&
                wing.IsActive)
            .GroupBy(wing => wing.PropertyId!.Value)
            .Select(group => new
            {
                PropertyId = group.Key,
                TotalWingCount = group.Select(x => x.WingId).Distinct().Count(),
                NoOfFlat = group.Sum(x => x.NoOfFlat ?? 0),
                NoOfShop = group.Sum(x => x.NoOfShop ?? 0)
            })
            .ToListAsync(cancellationToken);

        var societyDictionary = societyDetails
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.Id)
                    .First());

        var wingCountDictionary = wingCounts
            .ToDictionary(
                x => x.PropertyId,
                x => new
                {
                    x.TotalWingCount,
                    x.NoOfFlat,
                    x.NoOfShop
                });

        foreach (var property in properties)
        {
            if (societyDictionary.TryGetValue(property.Id, out var society))
            {
                property.SocietyName = society.SocietyName;
                property.BuilderName = society.BuilderName;
                property.SocietyAddress = society.SocietyAddress;
            }

            if (wingCountDictionary.TryGetValue(property.Id, out var counts))
            {
                property.WingCount = counts.TotalWingCount;
                property.FlatCount = counts.NoOfFlat;
                property.ShopCount = counts.NoOfShop;
            }
        }
    }

    private async Task AttachTotalAreaAsync(
        List<CreatedByUserPropertyResponseDto> properties,
        CancellationToken cancellationToken)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var propertyIds = properties
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var areaDetails = await _roomWiseRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(room =>
                room.PropertyId.HasValue &&
                propertyIds.Contains(room.PropertyId.Value) &&
                room.IsActive &&
                !room.MarkedForDeletion)
            .GroupBy(room => room.PropertyId!.Value)
            .Select(group => new
            {
                PropertyId = group.Key,
                TotalArea = group.Sum(x => x.TotalAreaSqMtr ?? 0)
            })
            .ToListAsync(cancellationToken);

        var areaDictionary = areaDetails
            .ToDictionary(
                x => x.PropertyId,
                x => x.TotalArea);

        foreach (var property in properties)
        {
            property.TotalArea = areaDictionary.TryGetValue(property.Id, out var totalArea)
                ? totalArea
                : 0;
        }
    }
}
