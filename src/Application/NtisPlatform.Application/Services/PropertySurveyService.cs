using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

public partial class PropertySurveyService : IPropertySurveyService
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

    private readonly IRepository<PropertyWorkflowDetailsEntity, int> _workflowDetailsRepository;
    private readonly IRepository<PropertyWorkflowStageMasterEntity, int> _workflowStageRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<CommonRemarkDetailsEntity, int>? _commonRemarkDetailsRepository;
    private readonly IRepository<PropertySurveyVisitEntity, int> _propertySurveyVisitRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertySurveyService> _logger;

    private readonly IRepository<PropertyAssessmentEntity, int>? _propertyAssessmentRepository;
    private readonly IRepository<WingDetailsMastEntity, int>? _wingDetailsMastRepository;
    private readonly IRepository<PropertyTypeCategoryEntity, int>? _propertyTypeCategoryRepository;
    private readonly IRepository<DocumentBindingEntity, int>? _documentBindingRepository;
    private readonly IRepository<DocumentEntity, int>? _documentRepository;

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
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseRepository,
        IRepository<PropertyWorkflowDetailsEntity, int> workflowDetailsRepository,
        IRepository<PropertyWorkflowStageMasterEntity, int> workflowStageRepository,
        IRepository<UserEntity, int> userRepository,
        IRepository<PropertySurveyVisitEntity, int> propertySurveyVisitRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertySurveyService> logger,
        IRepository<CommonRemarkDetailsEntity, int>? commonRemarkDetailsRepository = null,
        IRepository<PropertyAssessmentEntity, int>? propertyAssessmentRepository = null,
        IRepository<WingDetailsMastEntity, int>? wingDetailsMastRepository = null,
        IRepository<PropertyTypeCategoryEntity, int>? propertyTypeCategoryRepository = null,
        IRepository<DocumentBindingEntity, int>? documentBindingRepository = null,
        IRepository<DocumentEntity, int>? documentRepository = null)
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

        _workflowDetailsRepository = workflowDetailsRepository;
        _workflowStageRepository = workflowStageRepository;
        _userRepository = userRepository;
        _commonRemarkDetailsRepository = commonRemarkDetailsRepository;
        _propertySurveyVisitRepository = propertySurveyVisitRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _propertyAssessmentRepository = propertyAssessmentRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _propertyTypeCategoryRepository = propertyTypeCategoryRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
    }

    public async Task<UserPropertyPageDto> SearchNewlyCreatedPropertiesAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            throw new ArgumentException(
                "UserId is required and must be greater than zero.");
        }

        if (request.ModuleId <= 0)
        {
            throw new ArgumentException(
                "ModuleId is required and must be greater than zero.");
        }

        if (request.WardId <= 0)
        {
            throw new ArgumentException(
                "WardId is required and must be greater than zero.");
        }

        var module = await _moduleMasterRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.Id == request.ModuleId &&
                x.IsActive)
            .Select(x => new
            {
                x.ModuleCode,
                x.ModuleName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (module == null)
        {
            throw new ArgumentException(
                $"Invalid or inactive ModuleId: {request.ModuleId}");
        }

        var moduleKey = !string.IsNullOrWhiteSpace(module.ModuleCode)
            ? module.ModuleCode.Trim().ToUpperInvariant()
            : module.ModuleName?.Trim().ToUpperInvariant();

        return moduleKey switch
        {
            ModuleConstants.GeoSequencing =>
                await SearchGeoSequencingNewlyCreatedPropertiesAsync(
                    request,
                    cancellationToken),

            ModuleConstants.Survey =>
                await SearchSurveyNewlyCreatedPropertiesAsync(
                    request,
                    cancellationToken),

            ModuleConstants.Trade =>
                throw new NotSupportedException(
                    "Trade property search is not configured yet."),

            ModuleConstants.BillApp =>
                throw new NotSupportedException(
                    "Bill App property search is not configured yet."),

            ModuleConstants.Water =>
                throw new NotSupportedException(
                    "Water property search is not configured yet."),

            _ => string.IsNullOrWhiteSpace(moduleKey)
                ? await SearchGeoSequencingNewlyCreatedPropertiesAsync(
                    request,
                    cancellationToken)
                : throw new NotSupportedException(
                    $"Property search is not configured for module " +
                    $"'{module.ModuleName}' with code '{module.ModuleCode}'.")
        };
    }

    private async Task<UserPropertyPageDto>
    SearchGeoSequencingNewlyCreatedPropertiesAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var searchText = !string.IsNullOrWhiteSpace(request.SearchText)
            ? request.SearchText.Trim()
            : request.SearchTerm?.Trim();

        var skip = ValidateAndCalculateSkip(request.PageNumber, request.PageSize);

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
                    mapDetail != null && mapDetail.PropertyIdOld.HasValue
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
               && !_propertyTypeRepository.GetQueryable().Any(ptm => ptm.Id == property.PropertyTypeId && ptm.IsActive && ptm.PartType == PropertyPartTypes.Amenity)
               && (
                   (category == null || category.PropertyCategoryName == null || !category.PropertyCategoryName.Contains("Apartment"))
                   ||
                   string.IsNullOrWhiteSpace(property.PartitionNo)
               )

               // Exclude properties whose PartitionNo is a WingNo
               && !_societyWingRepository
                   .GetQueryable()
                   .Any(sw =>
                       (sw.PropertyId == property.Id ||
                        _societyRepository.GetQueryable().Any(s => s.Id == sw.SocietyDetailId && s.PropertyId == property.Id && s.IsActive && !s.MarkedForDeletion)) &&
                       sw.IsActive &&
                       _wingMasterRepository
                           .GetQueryable()
                           .Any(wing =>
                               wing.Id == sw.WingId &&
                               wing.IsActive &&
                               !string.IsNullOrWhiteSpace(property.PartitionNo) &&
                               wing.WingNo == property.PartitionNo))

            select new
            {
                Property = property,
                Ward = ward,
                Category = category,
                PropertyType = propertyType,
                OldProperty = oldProperty
            };

        var propertyTypeFilter = new List<int>();
        if (request.PropertyTypeId.HasValue)
        {
            propertyTypeFilter.Add(request.PropertyTypeId.Value);
        }
        if (request.PropertyTypeIds != null && request.PropertyTypeIds.Count > 0)
        {
            propertyTypeFilter.AddRange(request.PropertyTypeIds);
        }
        propertyTypeFilter = propertyTypeFilter.Distinct().ToList();

        if (propertyTypeFilter.Count > 0)
        {
            query = query.Where(x => x.Property.PropertyTypeId.HasValue && propertyTypeFilter.Contains(x.Property.PropertyTypeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
        {
            var propertyNoTrim = request.PropertyNo.Trim();
            query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo == propertyNoTrim);
        }

        if (!string.IsNullOrWhiteSpace(request.PartitionNo))
        {
            var partitionNoTrim = request.PartitionNo.Trim();
            query = query.Where(x => x.Property.PartitionNo != null && x.Property.PartitionNo == partitionNoTrim);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x =>
                (x.Property.PropertyNo != null &&
                 x.Property.PropertyNo.Contains(searchText)) ||

                (x.Property.PartitionNo != null &&
                 x.Property.PartitionNo.Contains(searchText)) ||

                (x.Category != null &&
                 x.Category.PropertyCategoryName != null &&
                 x.Category.PropertyCategoryName.Contains(searchText)) ||

                (x.PropertyType != null &&
                 x.PropertyType.PropertyDescription != null &&
                 x.PropertyType.PropertyDescription.Contains(searchText)) ||

                (x.Property.OwnerName != null &&
                 x.Property.OwnerName.Contains(searchText)) ||

                (x.Property.OccupierName != null &&
                 x.Property.OccupierName.Contains(searchText)) ||

                (x.Property.Address != null &&
                 x.Property.Address.Contains(searchText)) ||

                (x.Property.UPICId != null &&
                 x.Property.UPICId.Contains(searchText)) ||

                _societyRepository
                    .GetQueryable()
                    .Any(s =>
                        s.PropertyId.HasValue &&
                        s.PropertyId.Value == x.Property.Id &&
                        s.IsActive &&
                        (
                            (s.SocietyName != null &&
                             s.SocietyName.Contains(searchText)) ||

                            (s.BuilderName != null &&
                             s.BuilderName.Contains(searchText)) ||

                            (s.SocietyAddress != null &&
                             s.SocietyAddress.Contains(searchText))
                        )));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var maxWardPropertyId = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(property =>
                property.WardId == request.WardId &&
                property.PropertySeqNo.HasValue &&
                property.IsActive &&
                !property.MarkedForDeletion &&
                !_propertyTypeRepository.GetQueryable().Any(ptm => ptm.Id == property.PropertyTypeId && ptm.IsActive && ptm.PartType == PropertyPartTypes.Amenity) &&
                (
                    !_categoryRepository.GetQueryable().Any(c => c.Id == property.CategoryId && c.PropertyCategoryName != null && c.PropertyCategoryName.Contains("Apartment"))
                    ||
                    string.IsNullOrWhiteSpace(property.PartitionNo)
                ) &&

                // Exclude Wing properties while finding CanDelete property
                !_societyWingRepository
                    .GetQueryable()
                    .Any(sw =>
                        (sw.PropertyId == property.Id ||
                         _societyRepository.GetQueryable().Any(s => s.Id == sw.SocietyDetailId && s.PropertyId == property.Id && s.IsActive && !s.MarkedForDeletion)) &&
                        sw.IsActive &&
                        _wingMasterRepository
                            .GetQueryable()
                            .Any(wing =>
                                wing.Id == sw.WingId &&
                                wing.IsActive &&
                                property.PartitionNo != null &&
                                wing.WingNo == property.PartitionNo)))
            .OrderByDescending(property => property.PropertySeqNo)
            .ThenByDescending(property => property.Id)
            .Select(property => (int?)property.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var orderedQuery = query
            .OrderByDescending(x => x.Property.PropertySeqNo.HasValue)
            .ThenByDescending(x => x.Property.PropertySeqNo)
            .ThenBy(x => string.IsNullOrWhiteSpace(x.Property.PartitionNo) ? 0 : 1)
            .ThenByDescending(x => x.Property.Id);

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
                    PropertyTypeId = x.Property.PropertyTypeId,
                    FlatOrShopNo = x.Property.FlatOrShopNo,
                    Type = x.PropertyType != null ? x.PropertyType.Type : null,
                    CategoryId = x.Property.CategoryId,
                    CategoryName = x.Category != null ? x.Category.PropertyCategoryName : null,
                    PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                    OldALV = x.OldProperty != null ? x.OldProperty.OldALV : null,
                    OldRV = x.OldProperty != null ? x.OldProperty.OldRV : null,
                    OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                    OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                    OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                    OwnerName = x.Property.OwnerName,
                    OccupierName = x.Property.OccupierName,
                    Address = x.Property.Address,
                    UpicId = x.Property.UPICId,
                    CreatedBy = x.Property.CreatedBy,
                    CreatedDate = x.Property.CreatedDate,
                    CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value
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
                    PropertyTypeId = x.Property.PropertyTypeId,
                    FlatOrShopNo = x.Property.FlatOrShopNo,
                    Type = x.PropertyType != null ? x.PropertyType.Type : null,
                    CategoryId = x.Property.CategoryId,
                    CategoryName = x.Category != null ? x.Category.PropertyCategoryName : null,
                    PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                    OldALV = x.OldProperty != null ? x.OldProperty.OldALV : null,
                    OldRV = x.OldProperty != null ? x.OldProperty.OldRV : null,
                    OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                    OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                    OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                    OwnerName = x.Property.OwnerName,
                    OccupierName = x.Property.OccupierName,
                    Address = x.Property.Address,
                    UpicId = x.Property.UPICId,
                    CreatedBy = x.Property.CreatedBy,
                    CreatedDate = x.Property.CreatedDate,
                    CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value
                })
                .ToListAsync(cancellationToken);
        }

        await AttachPropertyMastDetailsAsync(data, cancellationToken);
        await AttachPropertyDocumentsAsync(data, cancellationToken);
        await AttachSocietyDetailsAsync(data, cancellationToken);
        await AttachTotalAreaAsync(data, cancellationToken);
        await AttachTotalAmenityCountAsync(data, cancellationToken);
        await AttachMapCountAsync(data, cancellationToken);

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
            Status = true,
            Message = data.Count == 0 ? "No properties found." : "Properties fetched successfully.",
            Count = data.Count,
            TotalCount = totalCount,
            PageNumber = responsePageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNext = hasNext,
            Data = data
        };
    }

    private async Task<UserPropertyPageDto>
    SearchSurveyNewlyCreatedPropertiesAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var searchText = !string.IsNullOrWhiteSpace(request.SearchText)
            ? request.SearchText.Trim()
            : request.SearchTerm?.Trim();

        var skip = ValidateAndCalculateSkip(request.PageNumber, request.PageSize);

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

            join propertyTypeCategory in (_propertyTypeCategoryRepository != null
                ? _propertyTypeCategoryRepository.GetQueryable().AsNoTracking()
                : Enumerable.Empty<PropertyTypeCategoryEntity>().AsQueryable())
                on property.CategoryId equals propertyTypeCategory.Id
                into propertyTypeCategoryGroup

            from propertyTypeCategory in propertyTypeCategoryGroup.DefaultIfEmpty()

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
                    mapDetail != null && mapDetail.PropertyIdOld.HasValue
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
               && !_propertyTypeRepository.GetQueryable().Any(ptm => ptm.Id == property.PropertyTypeId && ptm.IsActive && ptm.PartType == PropertyPartTypes.Amenity)
               && (
                   (category == null || category.PropertyCategoryName == null || !category.PropertyCategoryName.Contains("Apartment"))
                   ||
                   string.IsNullOrWhiteSpace(property.PartitionNo)
               )

               // Exclude properties whose PartitionNo is a WingNo
               && !_societyWingRepository
                   .GetQueryable()
                   .Any(sw =>
                       (sw.PropertyId == property.Id ||
                        _societyRepository.GetQueryable().Any(s => s.Id == sw.SocietyDetailId && s.PropertyId == property.Id && s.IsActive && !s.MarkedForDeletion)) &&
                       sw.IsActive &&
                       _wingMasterRepository
                           .GetQueryable()
                           .Any(wing =>
                               wing.Id == sw.WingId &&
                               wing.IsActive &&
                               !string.IsNullOrWhiteSpace(property.PartitionNo) &&
                               wing.WingNo == property.PartitionNo))

            select new
            {
                Property = property,
                Ward = ward,
                Category = category,
                PropertyType = propertyType,
                PropertyTypeCategory = propertyTypeCategory,
                OldProperty = oldProperty
            };

        var propertyTypeFilter = new List<int>();
        if (request.PropertyTypeId.HasValue)
        {
            propertyTypeFilter.Add(request.PropertyTypeId.Value);
        }
        if (request.PropertyTypeIds != null && request.PropertyTypeIds.Count > 0)
        {
            propertyTypeFilter.AddRange(request.PropertyTypeIds);
        }
        propertyTypeFilter = propertyTypeFilter.Distinct().ToList();

        if (propertyTypeFilter.Count > 0)
        {
            query = query.Where(x => x.Property.PropertyTypeId.HasValue && propertyTypeFilter.Contains(x.Property.PropertyTypeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
        {
            var propertyNoTrim = request.PropertyNo.Trim();
            query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo == propertyNoTrim);
        }

        if (!string.IsNullOrWhiteSpace(request.PartitionNo))
        {
            var partitionNoTrim = request.PartitionNo.Trim();
            query = query.Where(x => x.Property.PartitionNo != null && x.Property.PartitionNo == partitionNoTrim);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x =>
                (x.Property.PropertyNo != null &&
                 x.Property.PropertyNo.Contains(searchText)) ||

                (x.Property.PartitionNo != null &&
                 x.Property.PartitionNo.Contains(searchText)) ||

                (x.Category != null &&
                 x.Category.PropertyCategoryName != null &&
                 x.Category.PropertyCategoryName.Contains(searchText)) ||

                (x.PropertyType != null &&
                 x.PropertyType.PropertyDescription != null &&
                 x.PropertyType.PropertyDescription.Contains(searchText)) ||

                (x.Property.OwnerName != null &&
                 x.Property.OwnerName.Contains(searchText)) ||

                (x.Property.OwnerNameEnglish != null &&
                 x.Property.OwnerNameEnglish.Contains(searchText)) ||

                (x.Property.OccupierName != null &&
                 x.Property.OccupierName.Contains(searchText)) ||

                (x.Property.OccupierNameEnglish != null &&
                 x.Property.OccupierNameEnglish.Contains(searchText)) ||

                (x.Property.Address != null &&
                 x.Property.Address.Contains(searchText)) ||

                (x.Property.AddressEnglish != null &&
                 x.Property.AddressEnglish.Contains(searchText)) ||

                (x.Property.UPICId != null &&
                 x.Property.UPICId.Contains(searchText)) ||

                _societyRepository
                    .GetQueryable()
                    .Any(s =>
                        s.PropertyId.HasValue &&
                        s.PropertyId.Value == x.Property.Id &&
                        s.IsActive &&
                        (
                            (s.SocietyName != null &&
                             s.SocietyName.Contains(searchText)) ||

                            (s.BuilderName != null &&
                             s.BuilderName.Contains(searchText)) ||

                            (s.SocietyAddress != null &&
                             s.SocietyAddress.Contains(searchText))
                        )));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var maxWardPropertyId = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(property =>
                property.WardId == request.WardId &&
                property.PropertySeqNo.HasValue &&
                property.IsActive &&
                !property.MarkedForDeletion &&
                !_propertyTypeRepository.GetQueryable().Any(ptm => ptm.Id == property.PropertyTypeId && ptm.IsActive && ptm.PartType == PropertyPartTypes.Amenity) &&
                (
                    !_categoryRepository.GetQueryable().Any(c => c.Id == property.CategoryId && c.PropertyCategoryName != null && c.PropertyCategoryName.Contains("Apartment"))
                    ||
                    string.IsNullOrWhiteSpace(property.PartitionNo)
                ) &&

                // Exclude Wing properties while finding CanDelete property
                !_societyWingRepository
                    .GetQueryable()
                    .Any(sw =>
                        (sw.PropertyId == property.Id ||
                         _societyRepository.GetQueryable().Any(s => s.Id == sw.SocietyDetailId && s.PropertyId == property.Id && s.IsActive && !s.MarkedForDeletion)) &&
                        sw.IsActive &&
                        _wingMasterRepository
                            .GetQueryable()
                            .Any(wing =>
                                wing.Id == sw.WingId &&
                                wing.IsActive &&
                                property.PartitionNo != null &&
                                wing.WingNo == property.PartitionNo)))
            .OrderByDescending(property => property.PropertySeqNo)
            .ThenByDescending(property => property.Id)
            .Select(property => (int?)property.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var orderedQuery = query
            .OrderByDescending(x => x.Property.PropertySeqNo.HasValue)
            .ThenByDescending(x => x.Property.PropertySeqNo)
            .ThenBy(x => string.IsNullOrWhiteSpace(x.Property.PartitionNo) ? 0 : 1)
            .ThenByDescending(x => x.Property.Id);

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
                    PropertyTypeId = x.Property.PropertyTypeId,
                    FlatOrShopNo = x.Property.FlatOrShopNo,
                    Type = x.PropertyType != null ? x.PropertyType.Type : null,
                    CategoryId = x.Property.CategoryId,
                    CategoryName = x.PropertyTypeCategory != null
                        ? x.PropertyTypeCategory.PropertyTypeCategory
                        : (x.Category != null ? x.Category.PropertyCategoryName : null),
                    PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                    OldALV = x.OldProperty != null ? x.OldProperty.OldALV : null,
                    OldRV = x.OldProperty != null ? x.OldProperty.OldRV : null,
                    OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                    OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                    OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                    OwnerName = x.Property.OwnerName,
                    OccupierName = x.Property.OccupierName,
                    Address = x.Property.Address,
                    UpicId = x.Property.UPICId,
                    CreatedBy = x.Property.CreatedBy,
                    CreatedDate = x.Property.CreatedDate,
                    CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value
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
                    PropertyTypeId = x.Property.PropertyTypeId,
                    FlatOrShopNo = x.Property.FlatOrShopNo,
                    Type = x.PropertyType != null ? x.PropertyType.Type : null,
                    CategoryId = x.Property.CategoryId,
                    CategoryName = x.PropertyTypeCategory != null
                        ? x.PropertyTypeCategory.PropertyTypeCategory
                        : (x.Category != null ? x.Category.PropertyCategoryName : null),
                    PropertyDescription = x.PropertyType != null ? x.PropertyType.PropertyDescription : null,
                    OldALV = x.OldProperty != null ? x.OldProperty.OldALV : null,
                    OldRV = x.OldProperty != null ? x.OldProperty.OldRV : null,
                    OldGeneralTax = x.OldProperty != null ? x.OldProperty.OldGeneralTax : null,
                    OldTotalTax = x.OldProperty != null ? x.OldProperty.OldTotalTax : null,
                    OldConstructionArea = x.OldProperty != null ? x.OldProperty.OldConstructionArea : null,
                    OwnerName = x.Property.OwnerName,
                    OccupierName = x.Property.OccupierName,
                    Address = x.Property.Address,
                    UpicId = x.Property.UPICId,
                    CreatedBy = x.Property.CreatedBy,
                    CreatedDate = x.Property.CreatedDate,
                    CanDelete = maxWardPropertyId.HasValue && x.Property.Id == maxWardPropertyId.Value
                })
                .ToListAsync(cancellationToken);
        }

        await AttachPropertyMastDetailsAsync(data, cancellationToken);
        await AttachPropertyDocumentsAsync(data, cancellationToken);
        await AttachSocietyDetailsAsync(data, cancellationToken);
        await AttachTotalAreaAsync(data, cancellationToken);
        await AttachTotalAmenityCountAsync(data, cancellationToken);
        await AttachMapCountAsync(data, cancellationToken);

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
            Status = true,
            Message = data.Count == 0 ? "No properties found." : "Properties fetched successfully.",
            Count = data.Count,
            TotalCount = totalCount,
            PageNumber = responsePageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNext = hasNext,
            Data = data
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

        List<PropertySearchDocumentDto> propertyDocuments;

        if (_documentBindingRepository != null && _documentRepository != null)
        {
            var query1 = from photo in _propertyPhotoRepository.GetQueryable().AsNoTracking()
                         join binding in _documentBindingRepository.GetQueryable().AsNoTracking()
                             on photo.DocumentBindingId equals binding.Id
                         join document in _documentRepository.GetQueryable().AsNoTracking()
                             on binding.DocumentId equals document.Id
                         where photo.PropertyId.HasValue && propertyIds.Contains(photo.PropertyId.Value)
                            && photo.IsActive
                            && !photo.MarkedForDeletion
                            && binding.IsActive
                            && !binding.MarkedForDeletion
                            && binding.IsReferenceValid
                            && document.IsActive
                            && (
                                (document.MimeType != null && document.MimeType.StartsWith(ImageMimeTypePrefix)) ||
                                (document.FileExtension != null && SupportedImageExtensions.Contains(document.FileExtension.ToLower()))
                            )
                         select new PropertySearchDocumentDto
                         {
                             PropertyPhotoId = photo.Id,
                             PropertyId = photo.PropertyId ?? 0,
                             PhotoTypeId = photo.PhotoTypeId,
                             DocumentId = document.Id,
                             DocumentGuid = document.DocumentGuid,
                             OriginalFileName = document.OriginalFileName,
                             FileName = document.FileName,
                             MimeType = document.MimeType,
                             FileExtension = document.FileExtension,
                             FileSizeBytes = document.FileSizeBytes,
                             DocumentBindingId = photo.DocumentBindingId,
                             IsLatest = photo.IsLatest,
                             DisplayOrder = photo.DisplayOrder,
                             Remarks = photo.Remarks,
                             ViewUrl = $"/api/documents/{document.DocumentGuid}/view",
                             DownloadUrl = $"/api/documents/{document.DocumentGuid}/download"
                         };

            var query2 = from photo in _propertyPhotoRepository.GetQueryable().AsNoTracking()
                         join binding in _documentBindingRepository.GetQueryable().AsNoTracking()
                             on new { TableName = "PropertyPhoto", TableId = photo.Id } equals new { TableName = binding.ReferenceTableName, TableId = binding.ReferenceTableId ?? 0 }
                         join document in _documentRepository.GetQueryable().AsNoTracking()
                             on binding.DocumentId equals document.Id
                         where photo.PropertyId.HasValue && propertyIds.Contains(photo.PropertyId.Value)
                            && photo.IsActive
                            && !photo.MarkedForDeletion
                            && binding.IsActive
                            && !binding.MarkedForDeletion
                            && binding.IsReferenceValid
                            && document.IsActive
                            && (
                                (document.MimeType != null && document.MimeType.StartsWith(ImageMimeTypePrefix)) ||
                                (document.FileExtension != null && SupportedImageExtensions.Contains(document.FileExtension.ToLower()))
                            )
                         select new PropertySearchDocumentDto
                         {
                             PropertyPhotoId = photo.Id,
                             PropertyId = photo.PropertyId ?? 0,
                             PhotoTypeId = photo.PhotoTypeId,
                             DocumentId = document.Id,
                             DocumentGuid = document.DocumentGuid,
                             OriginalFileName = document.OriginalFileName,
                             FileName = document.FileName,
                             MimeType = document.MimeType,
                             FileExtension = document.FileExtension,
                             FileSizeBytes = document.FileSizeBytes,
                             DocumentBindingId = binding.Id,
                             IsLatest = photo.IsLatest,
                             DisplayOrder = photo.DisplayOrder,
                             Remarks = photo.Remarks,
                             ViewUrl = $"/api/documents/{document.DocumentGuid}/view",
                             DownloadUrl = $"/api/documents/{document.DocumentGuid}/download"
                         };

            var list1 = await query1.ToListAsync(cancellationToken);
            var list2 = await query2.ToListAsync(cancellationToken);

            propertyDocuments = list1
                .Concat(list2)
                .GroupBy(x => new { x.PropertyPhotoId, x.DocumentId })
                .Select(g => g.First())
                .OrderBy(x => x.PropertyId)
                .ThenByDescending(x => x.IsLatest)
                .ThenByDescending(x => x.DisplayOrder)
                .ThenByDescending(x => x.PropertyPhotoId)
                .ToList();
        }
        else
        {
            propertyDocuments = await
            (
                from photo in _propertyPhotoRepository
                    .GetQueryable()
                    .AsNoTracking()

                where photo.PropertyId.HasValue && propertyIds.Contains(photo.PropertyId.Value)
                      && photo.IsActive
                      && !photo.MarkedForDeletion
                      && photo.DocumentBinding != null
                      && photo.DocumentBinding.IsActive
                      && !photo.DocumentBinding.MarkedForDeletion
                      && photo.DocumentBinding.IsReferenceValid
                      && photo.DocumentBinding.Document != null
                      && photo.DocumentBinding.Document.IsActive
                      && (
                          (photo.DocumentBinding.Document.MimeType != null && photo.DocumentBinding.Document.MimeType.StartsWith(ImageMimeTypePrefix)) ||
                          (photo.DocumentBinding.Document.FileExtension != null && SupportedImageExtensions.Contains(photo.DocumentBinding.Document.FileExtension.ToLower()))
                      )
                orderby
                    photo.PropertyId,
                    photo.IsLatest descending,
                    photo.DisplayOrder descending,
                    photo.Id descending

                select new PropertySearchDocumentDto
                {
                    PropertyPhotoId = photo.Id,
                    PropertyId = photo.PropertyId ?? 0,
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
                    Remarks = photo.Remarks,
                    ViewUrl = $"/api/documents/{photo.DocumentBinding!.Document!.DocumentGuid}/view",
                    DownloadUrl = $"/api/documents/{photo.DocumentBinding!.Document!.DocumentGuid}/download"
                }
            )
            .ToListAsync(cancellationToken);
        }

        var documentsByPropertyId = propertyDocuments
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        foreach (var property in properties)
        {
            property.Documents =
                documentsByPropertyId.TryGetValue(
                    property.Id,
                    out var documents)
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
            .Where(x =>
                x.PropertyId.HasValue &&
                propertyIds.Contains(x.PropertyId.Value) &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .Select(x => new
            {
                x.Id,
                PropertyId = x.PropertyId!.Value,
                x.SocietyName,
                x.BuilderName,
                x.SocietyAddress
            })
            .ToListAsync(cancellationToken);

        var societyDetailIds = societyDetails
            .Select(x => x.Id)
            .ToList();

        var societyDetailToPropertyMap = societyDetails
            .ToDictionary(x => x.Id, x => x.PropertyId);

        var wingDetailsRaw = await _societyWingRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                ((x.PropertyId.HasValue && propertyIds.Contains(x.PropertyId.Value)) ||
                (x.SocietyDetailId.HasValue && societyDetailIds.Contains(x.SocietyDetailId.Value))))
            .Select(x => new
            {
                x.Id,
                PropertyId = x.PropertyId,
                SocietyDetailId = x.SocietyDetailId,
                x.WingId,
                WingName = x.NewWingName ?? x.OldWingName ?? (x.WingMaster != null ? x.WingMaster.WingNo : null),
                x.FromFloor,
                x.ToFloor,
                NoOfFlat = x.NoOfFlat ?? 0,
                NoOfShop = x.NoOfShop ?? 0,
                NoOfRowHouse = x.NoOfRowHouse ?? 0
            })
            .ToListAsync(cancellationToken);

        var wingDetailIds = wingDetailsRaw.Select(w => w.Id).Distinct().ToList();

        var wingPropertyCounts = await (
            from pm in _repository.GetQueryable().AsNoTracking()
            join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                on pm.PropertyTypeId equals ptm.Id
            where pm.IsActive && !pm.MarkedForDeletion &&
                  ptm.IsActive &&
                  pm.WingDetailId.HasValue && wingDetailIds.Contains(pm.WingDetailId.Value)
            group new { pm, ptm } by pm.WingDetailId!.Value into g
            select new
            {
                WingDetailId = g.Key,
                NoOfFlat = g.Count(x => x.ptm.Type != null && x.ptm.Type.ToUpper() == "R"),
                NoOfShop = g.Count(x => x.ptm.Type != null && x.ptm.Type.ToUpper() == "C")
            })
            .ToDictionaryAsync(x => x.WingDetailId, x => new { x.NoOfFlat, x.NoOfShop }, cancellationToken);

        var propertyWardIds = properties.Select(p => p.WardId).Distinct().ToList();
        var propertyNos = properties.Select(p => p.PropertyNo).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

        var directPropertyCounts = await (
            from pm in _repository.GetQueryable().AsNoTracking()
            join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                on pm.PropertyTypeId equals ptm.Id
            where pm.IsActive && !pm.MarkedForDeletion &&
                  ptm.IsActive &&
                  propertyWardIds.Contains(pm.WardId) &&
                  propertyNos.Contains(pm.PropertyNo!)
            group new { pm, ptm } by new { pm.WardId, pm.PropertyNo } into g
            select new
            {
                g.Key.WardId,
                PropertyNo = g.Key.PropertyNo!,
                NoOfFlat = g.Count(x => x.ptm.Type != null && x.ptm.Type.ToUpper() == "R"),
                NoOfShop = g.Count(x => x.ptm.Type != null && x.ptm.Type.ToUpper() == "C")
            })
            .ToDictionaryAsync(x => (x.WardId, x.PropertyNo), x => new { x.NoOfFlat, x.NoOfShop }, cancellationToken);

        var wingListMap = wingDetailsRaw
            .Select(w =>
            {
                int targetPropertyId = w.PropertyId.HasValue
                    ? w.PropertyId.Value
                    : (w.SocietyDetailId.HasValue && societyDetailToPropertyMap.TryGetValue(w.SocietyDetailId.Value, out var propId) ? propId : 0);

                int dynamicNoOfFlat = wingPropertyCounts.TryGetValue(w.Id, out var wingCountsObj) && wingCountsObj != null ? wingCountsObj.NoOfFlat : 0;
                int dynamicNoOfShop = wingPropertyCounts.TryGetValue(w.Id, out wingCountsObj) && wingCountsObj != null ? wingCountsObj.NoOfShop : 0;

                return new
                {
                    TargetPropertyId = targetPropertyId,
                    WingDto = new NewlyCreatedPropertyWingDetailDto
                    {
                        Id = w.Id,
                        SocietyDetailId = w.SocietyDetailId,
                        PropertyId = w.PropertyId,
                        WingId = w.WingId,
                        WingName = w.WingName,
                        FromFloor = w.FromFloor,
                        ToFloor = w.ToFloor,
                        NoOfFlat = dynamicNoOfFlat,
                        NoOfShop = dynamicNoOfShop,
                        NoOfRowHouse = w.NoOfRowHouse
                    }
                };
            })
            .Where(x => x.TargetPropertyId > 0 && propertyIds.Contains(x.TargetPropertyId))
            .GroupBy(x => x.TargetPropertyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(x => x.WingDto.Id)
                    .Select(x => x.First().WingDto)
                    .ToList());

        var wingCounts = wingListMap
            .Select(kvp => new
            {
                PropertyId = kvp.Key,
                TotalWingCount = kvp.Value.Select(w => w.WingId).Where(id => id.HasValue).Distinct().Count(),
                NoOfFlat = kvp.Value.Sum(w => w.NoOfFlat),
                NoOfShop = kvp.Value.Sum(w => w.NoOfShop),
                NoOfRowHouse = kvp.Value.Sum(w => w.NoOfRowHouse)
            })
            .ToList();

        var societyDictionary = societyDetails
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.SocietyName) ? 1 : 0)
                    .ThenByDescending(x => x.Id)
                    .First());

        var wingCountDictionary = wingCounts
            .ToDictionary(
                x => x.PropertyId,
                x => new
                {
                    x.TotalWingCount,
                    x.NoOfFlat,
                    x.NoOfShop,
                    x.NoOfRowHouse
                });

        foreach (var property in properties)
        {
            if (societyDictionary.TryGetValue(
                    property.Id,
                    out var society))
            {
                property.SocietyName = society.SocietyName;
                property.BuilderName = society.BuilderName;
                property.SocietyAddress = society.SocietyAddress;
            }

            if (wingCountDictionary.TryGetValue(
                    property.Id,
                    out var counts))
            {
                property.TotalWingCount = counts.TotalWingCount;
                property.NoOfFlat = counts.NoOfFlat;
                property.NoOfShop = counts.NoOfShop;
                property.NoOfRowHouse = counts.NoOfRowHouse;
            }
            else if (!string.IsNullOrWhiteSpace(property.PropertyNo) &&
                     directPropertyCounts.TryGetValue((property.WardId, property.PropertyNo), out var directCounts))
            {
                property.NoOfFlat = directCounts.NoOfFlat;
                property.NoOfShop = directCounts.NoOfShop;
                property.TotalWingCount = 0;
                property.NoOfRowHouse = 0;
            }
            else
            {
                property.NoOfFlat = 0;
                property.NoOfShop = 0;
                property.TotalWingCount = 0;
                property.NoOfRowHouse = 0;
            }

            if (wingListMap.TryGetValue(
                    property.Id,
                    out var wings))
            {
                property.Wings = wings;
            }
        }
    }

    private async Task AttachTotalAmenityCountAsync(
        List<CreatedByUserPropertyResponseDto> properties,
        CancellationToken cancellationToken)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var wardPropertyPairs = properties
            .Where(x => !string.IsNullOrWhiteSpace(x.PropertyNo))
            .Select(x => new { x.WardId, PropertyNo = x.PropertyNo!.Trim() })
            .Distinct()
            .ToList();

        if (wardPropertyPairs.Count == 0)
        {
            return;
        }

        var wardIds = wardPropertyPairs.Select(x => x.WardId).Distinct().ToList();
        var propertyNos = wardPropertyPairs.Select(x => x.PropertyNo).Distinct().ToList();

        var amenityCounts = await (
            from pm in _repository.GetQueryable().AsNoTracking()
            join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                on pm.PropertyTypeId equals ptm.Id
            where wardIds.Contains(pm.WardId) &&
                  pm.PropertyNo != null &&
                  propertyNos.Contains(pm.PropertyNo) &&
                  pm.IsActive &&
                  !pm.MarkedForDeletion &&
                  ptm.IsActive &&
                  ptm.PartType == PropertyPartTypes.Amenity
            group pm by new { pm.WardId, pm.PropertyNo } into g
            select new
            {
                g.Key.WardId,
                PropertyNo = g.Key.PropertyNo!,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var amenityCountDict = amenityCounts
            .ToDictionary(
                x => (x.WardId, x.PropertyNo),
                x => x.Count);

        foreach (var property in properties)
        {
            if (!string.IsNullOrWhiteSpace(property.PropertyNo) &&
                amenityCountDict.TryGetValue((property.WardId, property.PropertyNo.Trim()), out var count))
            {
                property.TotalAmenityCount = count;
            }
        }
    }

    private async Task AttachMapCountAsync(
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

        var mapCounts = await _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.PropertyIdNew.HasValue && propertyIds.Contains(x.PropertyIdNew.Value) && x.Status == "ACTIVE" && x.IsActive)
            .GroupBy(x => x.PropertyIdNew!.Value)
            .Select(g => new
            {
                PropertyId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.PropertyId, x => x.Count, cancellationToken);

        foreach (var property in properties)
        {
            if (mapCounts.TryGetValue(property.Id, out var count))
            {
                property.MapCount = count;
            }
            else
            {
                property.MapCount = 0;
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

    private async Task AttachPropertyMastDetailsAsync(
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

        List<PropertyAssessmentEntity> assessmentDetails;

        if (_propertyAssessmentRepository != null)
        {
            assessmentDetails = await _propertyAssessmentRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(d => propertyIds.Contains(d.PropertyId) && d.IsActive && !d.MarkedForDeletion)
                .OrderByDescending(d => d.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            assessmentDetails = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(p => propertyIds.Contains(p.Id))
                .SelectMany(p => p.PropertyMastDetails.Where(d => d.IsActive && !d.MarkedForDeletion))
                .OrderByDescending(d => d.Id)
                .ToListAsync(cancellationToken);
        }

        var detailsByPropertyId = assessmentDetails
            .GroupBy(d => d.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.First());

        foreach (var property in properties)
        {
            if (detailsByPropertyId.TryGetValue(property.Id, out var detail))
            {
                property.BlockNo = detail.BlockNo;
                property.BHK = detail.BHK;
            }
        }
    }
}

