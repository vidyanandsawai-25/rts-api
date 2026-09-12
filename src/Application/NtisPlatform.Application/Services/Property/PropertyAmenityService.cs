using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyAmenity;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.Text.RegularExpressions;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Service implementation for property amenity operations.
/// </summary>
public class PropertyAmenityService : IPropertyAmenityService
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _propertyPhotoRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<GlobalSurveyWardAllocationEntity, int> _wardAllocationRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly IRepository<PropertyWorkflowStageMasterEntity, int> _workflowStageRepository;
    private readonly IRepository<SocietyWingDetailsEntity, int> _societyWingRepository;
    private readonly IRepository<WingDetailsMastEntity, int>? _wingDetailsRepository;
    private readonly IRepository<PropertyWorkflowDetailsEntity, int> _propertyWorkflowDetailsRepository;
    private readonly IRepository<PropertySurveyVisitEntity, int> _propertySurveyVisitRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertyAmenityService> _logger;

    public PropertyAmenityService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<PropertyPhotoEntity, int> propertyPhotoRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IRepository<GlobalSurveyWardAllocationEntity, int> wardAllocationRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
        IRepository<PropertyWorkflowStageMasterEntity, int> workflowStageRepository,
        IRepository<SocietyWingDetailsEntity, int> societyWingRepository,
        IRepository<PropertyWorkflowDetailsEntity, int> propertyWorkflowDetailsRepository,
        IRepository<PropertySurveyVisitEntity, int> propertySurveyVisitRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PropertyAmenityService> logger,
        IRepository<WingDetailsMastEntity, int>? wingDetailsRepository = null)
    {
        _propertyRepository = propertyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
        _wardAllocationRepository = wardAllocationRepository;
        _assessmentRepository = assessmentRepository;
        _workflowStageRepository = workflowStageRepository;
        _societyWingRepository = societyWingRepository;
        _propertyWorkflowDetailsRepository = propertyWorkflowDetailsRepository;
        _propertySurveyVisitRepository = propertySurveyVisitRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _wingDetailsRepository = wingDetailsRepository;
    }

    /// <summary>
    /// Retrieves all amenities based on ward, property number prefix and generic filters.
    /// </summary>
    public async Task<PagedResult<AmenityPropertyDto>> GetAllAmenitiesAsync(AmenityQueryParameters request, CancellationToken cancellationToken = default)
    {
        // 1. Check if the requested ward is actively allocated to the user
        var isWardAllocated = await _wardAllocationRepository.GetQueryable()
            .AnyAsync(x => x.UserId == request.UserId && x.WardId == request.WardId && x.IsActive, cancellationToken);

        if (!isWardAllocated)
        {
            var normalizedPageSize = request.PageSize == -1 ? 1 : request.PageSize;
            return new PagedResult<AmenityPropertyDto>(Enumerable.Empty<AmenityPropertyDto>(), 0, request.PageNumber, normalizedPageSize);
        }

        // 2. Build base queries with NoTracking and filtering
        var pmQuery = _propertyRepository.GetQueryable()
            .AsNoTracking()
            .Where(pm => pm.WardId == request.WardId && pm.IsActive && !pm.MarkedForDeletion);

        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
        {
            pmQuery = pmQuery.Where(pm => EF.Functions.Like(pm.PropertyNo, $"%{request.PropertyNo}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.PartitionNo))
        {
            pmQuery = pmQuery.Where(pm => EF.Functions.Like(pm.PartitionNo, $"%{request.PartitionNo}%"));
        }

        var ptmQuery = _propertyTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(ptm => ptm.PartType == PartTypeConstants.Amenity && ptm.Type == PartTypeConstants.Residential && ptm.IsActive);

        var pdQuery = _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.IsActive && !pd.MarkedForDeletion);

        var ppQuery = _propertyPhotoRepository.GetQueryable()
            .AsNoTracking()
            .Where(pp => pp.IsActive && !pp.MarkedForDeletion);

        var dbQuery = _documentBindingRepository.GetQueryable()
            .AsNoTracking()
            .Where(db => db.IsActive && !db.MarkedForDeletion && db.IsReferenceValid);

        var dQuery = _documentRepository.GetQueryable()
            .AsNoTracking()
            .Where(d => d.IsActive && !d.MarkedForDeletion);

        // 3. Perform step-by-step debugger-friendly Join and DTO mapping using LINQ method syntax
        var propertyTypeJoinedQuery = pmQuery
            .Join(ptmQuery, pm => pm.PropertyTypeId, ptm => ptm.Id, (pm, ptm) => pm);

        var propertyAndDetailsQuery = propertyTypeJoinedQuery
            .Join(pdQuery, pm => pm.Id, pd => pd.PropertyId, (pm, pd) => new { Property = pm, Details = pd });

        var photoJoinedQuery = propertyAndDetailsQuery
            .GroupJoin(ppQuery, x => x.Property.Id, pp => pp.PropertyId, (x, photos) => new { x.Property, x.Details, Photos = photos })
            .SelectMany(x => x.Photos.DefaultIfEmpty(), (x, photo) => new { x.Property, x.Details, Photo = photo });

        var bindingJoinedQuery = photoJoinedQuery
            .GroupJoin(dbQuery, x => x.Photo != null ? x.Photo.DocumentBindingId : null, db => (int?)db.Id, (x, bindings) => new { x.Property, x.Details, x.Photo, Bindings = bindings })
            .SelectMany(x => x.Bindings.DefaultIfEmpty(), (x, binding) => new { x.Property, x.Details, x.Photo, Binding = binding });

        var documentJoinedQuery = bindingJoinedQuery
            .GroupJoin(dQuery, x => x.Binding != null ? (int?)x.Binding.DocumentId : null, d => (int?)d.Id, (x, docs) => new { x.Property, x.Details, x.Photo, x.Binding, Docs = docs })
            .SelectMany(x => x.Docs.DefaultIfEmpty(), (x, doc) => new { x.Property, x.Details, x.Photo, x.Binding, Document = doc });

        var joinedQuery = documentJoinedQuery.Select(x => new AmenityPropertyDto
        {
            Id = x.Property.Id,
            PropertyDetailsId = x.Details.Id,
            TaxZoneId = x.Property.TaxZoneId,
            PropertyTypeId = x.Property.PropertyTypeId,
            PropertyNo = x.Property.PropertyNo,
            PartitionNo = x.Property.PartitionNo,
            CategoryId = x.Property.CategoryId,
            NoOfFloorAttachToAmenity = _propertyDetailsRepository.GetQueryable().Count(pd => pd.PropertyId == x.Property.Id && pd.IsActive && !pd.MarkedForDeletion),
            FloorId = x.Details.FloorId,
            SubFloorId = x.Details.SubFloorId,
            ConstructionTypeId = x.Details.ConstructionTypeId,
            TypeOfUseId = x.Details.TypeOfUseId,
            SubTypeOfUseId = x.Details.SubTypeOfUseId,
            NoOfRooms = x.Details.NoOfRooms,
            ConstructionYear = x.Details.ConstructionYear,
            AssessmentYear = x.Details.AssessmentYear,
            CarpetAreaSqMeter = x.Details.CarpetAreaSqMeter != null ? Math.Round(x.Details.CarpetAreaSqMeter.Value, 2) : (double?)null,
            CarpetAreaSqFeet = x.Details.CarpetAreaSqFeet != null ? Math.Round(x.Details.CarpetAreaSqFeet.Value, 2) : (double?)null,
            BuiltupAreaSqMeter = x.Details.BuiltupAreaSqMeter != null ? Math.Round(x.Details.BuiltupAreaSqMeter.Value, 2) : (double?)null,
            BuiltupAreaSqFeet = x.Details.BuiltupAreaSqFeet != null ? Math.Round(x.Details.BuiltupAreaSqFeet.Value, 2) : (double?)null,
            DocumentType = x.Document != null ? x.Document.DocumentType : null,
            DocumentBindingId = x.Photo != null ? x.Photo.DocumentBindingId : null,
            DocumentGuid = x.Document != null ? (Guid?)x.Document.DocumentGuid : null,
            // Optimized IsVerified flag - checks if InternalSurveyVerified is true
            IsVerified = _propertyWorkflowDetailsRepository.GetQueryable()
                .Where(pwfd => pwfd.PropertyId == x.Property.Id && pwfd.IsActive)
                .Join(_propertySurveyVisitRepository.GetQueryable().Where(psv => psv.IsActive),
                    pwfd => pwfd.Id,
                    psv => psv.PropertyWorkflowDetailsId,
                    (pwfd, psv) => psv.InternalSurveyVerified)
                .Any(verified => verified == true),
            // Optimized IsMerged flag - checks if PropertyMapDetail record exists
            IsMerged = _propertyMapDetailRepository.GetQueryable()
                .Any(pmd => pmd.PropertyIdNew == x.Property.Id
                    && pmd.IsActive
                    && pmd.Status == "ACTIVE")
        });

        // 4. Count total records before pagination
        var totalCount = await joinedQuery.CountAsync(cancellationToken);

        // 5. Apply pagination
        var paginatedQuery = joinedQuery;
        if (request.PageSize != -1)
        {
            paginatedQuery = paginatedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        var items = await paginatedQuery.ToListAsync(cancellationToken);

        // Normalize pagination metadata when PageSize == -1 (unpaged mode).
        var effectivePageSize = request.PageSize == -1 ? Math.Max(1, totalCount) : request.PageSize;
        var effectivePageNumber = request.PageSize == -1 ? 1 : request.PageNumber;
        return new PagedResult<AmenityPropertyDto>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Soft-deletes both the main Property and its associated PropertyDetails.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // 1. Find and delete associated PropertyDetails using the generic repository method
        var propertyDetails = await _propertyDetailsRepository.GetQueryable()
            .Where(x => x.PropertyId == id && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        foreach (var pd in propertyDetails)
        {
            await _propertyDetailsRepository.DeleteAsync(pd, cancellationToken);
        }

        // 2. Delete the main PropertyEntity
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property == null)
            return false;

        await _propertyRepository.DeleteAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Updates amenity-related data across Property, PropertyDetails, Assessment, and Workflow tables
    /// within a single transaction.
    /// </summary>
    public async Task<ApiResponse<AmenityPropertyDto>> UpdateAmenityAsync(int propertyId, UpdateAmenityDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Load the target property (including workflow history) and validate existence.
        var property = await _propertyRepository.GetQueryable()
            .Include(x => x.WorkflowHistory)
            .FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (property == null)
            return new ApiResponse<AmenityPropertyDto> { Success = false, Message = "Property not found." };

        // Step 1b: Validate that the target property is an Amenity type.
        var currentPropertyType = await _propertyTypeRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == property.PropertyTypeId && x.IsActive, cancellationToken);

        if (currentPropertyType == null || currentPropertyType.PartType != PartTypeConstants.Amenity)
            return new ApiResponse<AmenityPropertyDto> { Success = false, Message = "Property is not an amenity type." };

        // Step 2: Load current active PropertyDetails row (if available).
        var propertyDetails = await _propertyDetailsRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        // Step 3: Begin transaction to keep all related updates atomic.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 4: Update base Property fields.
            property.TaxZoneId = dto.TaxZoneId;

            // Validate that the requested PropertyTypeId is an active amenity type.
            if (dto.PropertyTypeId.HasValue)
            {
                var requestedType = await _propertyTypeRepository.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.PropertyTypeId.Value && x.IsActive, cancellationToken);

                if (requestedType == null || requestedType.PartType != PartTypeConstants.Amenity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.DiscardChanges();
                    return new ApiResponse<AmenityPropertyDto>
                    {
                        Success = false,
                        Message = "Invalid or non-amenity PropertyTypeId."
                    };
                }

                property.PropertyTypeId = dto.PropertyTypeId;
            }

            if (dto.PartitionNo != null) property.PartitionNo = dto.PartitionNo;
            if (dto.CategoryId.HasValue) property.CategoryId = dto.CategoryId;
            if (dto.OpenPlot.HasValue) property.OpenPlot = dto.OpenPlot;
            if (dto.UpdatedBy.HasValue) property.UpdatedBy = dto.UpdatedBy;
            property.UpdatedDate = DateTime.Now;

            if (dto.PropertySeqNo.HasValue)
            {
                property.PropertySeqNo = dto.PropertySeqNo.Value;
            }

            await _propertyRepository.UpdateAsync(property, cancellationToken);

            // Step 5: Update PropertyDetails from DTO when details row exists.
            if (propertyDetails != null)
            {
                _mapper.Map(dto, propertyDetails);

                if (dto.PropertyFloorId.HasValue)
                {
                    propertyDetails.FloorId = dto.PropertyFloorId.Value;
                }

                await _propertyDetailsRepository.UpdateAsync(propertyDetails, cancellationToken);
            }

            // Step 5b: Persist NoOfFlat / NoOfShop through SocietyWingDetails.
            if (dto.NoOfFlat.HasValue || dto.NoOfShop.HasValue)
            {
                var societyWingRecord = await _societyWingRepository.GetQueryable()
                    .Where(x => x.PropertyId == propertyId && x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (societyWingRecord != null)
                {
                    if (dto.NoOfFlat.HasValue) societyWingRecord.NoOfFlat = dto.NoOfFlat.Value;
                    if (dto.NoOfShop.HasValue) societyWingRecord.NoOfShop = dto.NoOfShop.Value;
                    if (dto.UpdatedBy.HasValue) societyWingRecord.UpdatedBy = dto.UpdatedBy;
                    societyWingRecord.UpdatedDate = DateTime.Now;
                    await _societyWingRepository.UpdateAsync(societyWingRecord, cancellationToken);
                }
            }

            // Step 6: Update BHK in PropertyAssessmentEntity when BHK is provided.
            if (!string.IsNullOrWhiteSpace(dto.Bhk))
            {
                var existingAssessment = await _assessmentRepository.GetQueryable()
                    .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

                if (existingAssessment != null)
                {
                    existingAssessment.BHK = dto.Bhk.Trim();
                    if (dto.UpdatedBy.HasValue) existingAssessment.UpdatedBy = dto.UpdatedBy;
                    existingAssessment.UpdatedDate = DateTime.Now;
                    await _assessmentRepository.UpdateAsync(existingAssessment, cancellationToken);
                }
            }

            // Step 7: Validate and update workflow stage when WorkflowStageId is provided.
            if (dto.WorkflowStageId.HasValue)
            {
                var isValidStage = await _workflowStageRepository.GetQueryable()
                    .AnyAsync(x => x.Id == dto.WorkflowStageId.Value && x.IsActive, cancellationToken);

                // Invalid stage: rollback and return business error response.
                if (!isValidStage)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.DiscardChanges();
                    return new ApiResponse<AmenityPropertyDto>
                    {
                        Success = false,
                        Message = "Invalid WorkflowStageId."
                    };
                }

                // Update latest active workflow row, or create one if missing.
                var workflowRow = property.WorkflowHistory
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                if (workflowRow == null)
                {
                    property.WorkflowHistory.Add(new PropertyWorkflowDetailsEntity
                    {
                        PropertyId = propertyId,
                        WorkflowStageId = dto.WorkflowStageId.Value,
                        CreatedBy = dto.UpdatedBy,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    });
                }
                else
                {
                    workflowRow.WorkflowStageId = dto.WorkflowStageId.Value;
                    if (dto.UpdatedBy.HasValue) workflowRow.UpdatedBy = dto.UpdatedBy;
                    workflowRow.UpdatedDate = DateTime.Now;
                }
            }

            // Step 8: Persist all changes and commit transaction.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Step 9: Fetch additional read-model data needed for response DTO.
            var activeWorkflow = property.WorkflowHistory
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            var societyWing = await _societyWingRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var assessmentSummary = await _assessmentRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new { x.BHK })
                .FirstOrDefaultAsync(cancellationToken);

            // Step 10: Build response DTO from updated entities.
            var responseDto = _mapper.Map<AmenityPropertyDto>(property);
            if (propertyDetails != null)
            {
                _mapper.Map(propertyDetails, responseDto);
            }

            // Step 11: Populate additional computed/related response fields.
            responseDto.OpenPlot = property.OpenPlot;
            responseDto.PropertySeqNo = property.PropertySeqNo;
            responseDto.WorkflowStageId = activeWorkflow?.WorkflowStageId;
            responseDto.Bhk = assessmentSummary?.BHK;
            responseDto.NoOfFlat = societyWing?.NoOfFlat;
            responseDto.NoOfShop = societyWing?.NoOfShop;
            responseDto.PropertyFloorId = propertyDetails?.FloorId;

            // Step 12: Return success response.
            return new ApiResponse<AmenityPropertyDto>
            {
                Success = true,
                Message = "Amenity updated successfully.",
                Items = responseDto
            };
        }
        catch
        {
            // Step 13: Ensure transaction rollback on any exception, then rethrow.
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    /// <summary>
    /// Gets the maximum amenity partition number and calculates the next available one.
    /// Supports WingDetailsId based Amenity numbering.
    /// </summary>
    public async Task<ApiResponse<GetMaxPropertyAmenityResponseDto>> GetMaxPropertyAmenityAsync(MaxPropertyAmenityQueryParameters request, CancellationToken cancellationToken = default)
    {
        #region Validate Ward Allocation

        var isWardAllocated = await _wardAllocationRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == request.UserId &&
                x.WardId == request.WardId &&
                x.IsActive,
                cancellationToken);

        if (!isWardAllocated)
        {
            return new ApiResponse<GetMaxPropertyAmenityResponseDto>
            {
                Success = false,
                Message = "Given ward is not allocated to this user.",
                Items = new GetMaxPropertyAmenityResponseDto
                {
                    PropertyNo = request.PropertyNo,
                    WingDetailsId = request.WingDetailsId
                }
            };
        }

        #endregion

        #region Validate Wing

        if (request.WingDetailsId.HasValue && request.WingDetailsId.Value > 0)
        {
            if (_wingDetailsRepository == null)
            {
                return new ApiResponse<GetMaxPropertyAmenityResponseDto>
                {
                    Success = false,
                    Message = "Wing repository is not available.",
                    Items = null
                };
            }

            var isWingValid = await _wingDetailsRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == request.WingDetailsId.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion,
                    cancellationToken);

            if (!isWingValid)
            {
                return new ApiResponse<GetMaxPropertyAmenityResponseDto>
                {
                    Success = false,
                    Message = "WingDetailsId is Invalid.",
                    Items = null
                };
            }
        }

        #endregion

        #region Step 1 : PropertyMast Query (Equivalent to DistinctPartitions)

        var pmQuery = _propertyRepository.GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.WardId == request.WardId &&
                x.PropertyNo == request.PropertyNo &&
                x.IsActive &&
                !x.MarkedForDeletion &&
                !string.IsNullOrWhiteSpace(x.PartitionNo) &&
                EF.Functions.Like(x.PartitionNo!, "%[0-9]%"));

        if (request.WingDetailsId.HasValue && request.WingDetailsId.Value > 0)
        {
            pmQuery = pmQuery.Where(x => x.WingDetailId == request.WingDetailsId.Value);
        }

        #endregion

        #region Step 2 : Join PropertyTypeMaster (Amenity)

        var ptmQuery = _propertyTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.PartType == PartTypeConstants.Amenity &&
                x.Type == PartTypeConstants.Residential &&
                x.IsActive);

        var amenityQuery = pmQuery.Join(
            ptmQuery,
            pm => pm.PropertyTypeId,
            ptm => ptm.Id,
            (pm, ptm) => pm);

        #endregion

        #region Step 3 : Distinct Partitions

        var distinctPartitions = await amenityQuery
            .Select(x => new
            {
                x.Id,
                x.PropertyNo,
                x.WingDetailId,
                x.PartitionNo
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        #endregion

        #region Step 4 : Parse Partition Number (Equivalent to SQL Prefix/Number)

        var regex = new Regex(@"^(\D+)(\d+)$", RegexOptions.Compiled);

        var parsedPartitions = new List<(
            int Id,
            string PropertyNo,
            int? WingDetailId,
            string PartitionNo,
            string Prefix,
            int Number)>();

        foreach (var partition in distinctPartitions)
        {
            var value = partition.PartitionNo!.Trim().ToUpper();

            var match = regex.Match(value);

            if (!match.Success)
                continue;

            // Reject non-AM prefixes to avoid counting non-amenity partitions.
            if (!string.Equals(match.Groups[1].Value, "AM", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(match.Groups[2].Value, out var number))
                continue;

            parsedPartitions.Add((
                partition.Id,
                partition.PropertyNo!,
                partition.WingDetailId,
                value,
                match.Groups[1].Value,
                number));
        }

        #endregion

        #region Step 5 : Get Max Amenity

        var maxEntry = parsedPartitions
            .OrderByDescending(x => x.Number)
            .Select(max => new
            {
                max.Id,
                max.PropertyNo,
                max.WingDetailId,
                max.PartitionNo,
                max.Prefix,
                LastAmenityNumber = max.Number,
                NextAmenity = $"{max.Prefix}{max.Number + 1}"
            })
            .FirstOrDefault();

        #endregion

        #region Step 6 : No Amenity Found

        if (maxEntry == null)
        {
            return new ApiResponse<GetMaxPropertyAmenityResponseDto>
            {
                Success = true,
                Message = "No amenity partitions found. Starting with AM1.",
                Items = new GetMaxPropertyAmenityResponseDto
                {
                    PropertyId = null,
                    PropertyNo = request.PropertyNo,
                    WingDetailsId = request.WingDetailsId,
                    Prefix = "AM",
                    LastPartitionNumber = 0,
                    LastAmenityPartition = null,
                    NextPartitionNumber = 1,
                    NextAmenityPartition = "AM1"
                }
            };
        }

        #endregion

        #region Step 7 : Success Response

        return new ApiResponse<GetMaxPropertyAmenityResponseDto>
        {
            Success = true,
            Message = "Max amenity partition fetched successfully.",
            Items = new GetMaxPropertyAmenityResponseDto
            {
                PropertyId = maxEntry.Id,
                PropertyNo = request.PropertyNo,
                WingDetailsId = request.WingDetailsId,
                Prefix = maxEntry.Prefix,
                LastPartitionNumber = maxEntry.LastAmenityNumber,
                LastAmenityPartition = maxEntry.PartitionNo,
                NextPartitionNumber = maxEntry.LastAmenityNumber + 1,
                NextAmenityPartition = maxEntry.NextAmenity
            }
        };

        #endregion
    }
}
