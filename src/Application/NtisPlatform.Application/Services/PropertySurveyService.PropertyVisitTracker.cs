using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyVisitTracker;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services;

public partial class PropertySurveyService : IPropertyVisitTrackerService
{
    public async Task<CreatePropertyVisitTrackerResponseDto>
        CreateVisitAsync(
            CreatePropertyVisitTrackerDto request,
            int loggedInUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Logged-in user information is invalid.");
        }

        var propertyExists = await _repository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == request.PropertyId &&
                    x.IsActive &&
                    !x.MarkedForDeletion,
                cancellationToken);

        if (!propertyExists)
        {
            throw new KeyNotFoundException(
                $"Property with ID {request.PropertyId} was not found.");
        }

        var workflowStage = await _workflowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.Id == request.WorkflowStageId &&
                x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.StageName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (workflowStage == null)
        {
            throw new ArgumentException(
                "Invalid or inactive workflow stage.");
        }

        try
        {
            var visitDateTime = DateTime.Now;

            var workflowDetails =
                new PropertyWorkflowDetailsEntity
                {
                    PropertyId = request.PropertyId,
                    WorkflowStageId = request.WorkflowStageId,
                    ModuleId = request.ModuleId,
                    IsActive = true,
                    CreatedBy = loggedInUserId,
                    CreatedDate = visitDateTime,
                    UpdatedBy = null,
                    UpdatedDate = null
                };

            await _workflowDetailsRepository.AddAsync(
                workflowDetails,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new CreatePropertyVisitTrackerResponseDto
            {
                Status = true,
                Message = "Property visit recorded successfully.",
                VisitId = workflowDetails.Id,
                PropertyId = workflowDetails.PropertyId,
                WorkflowStageId = workflowDetails.WorkflowStageId,
                WorkflowStageName = workflowStage.StageName,
                ModuleId = workflowDetails.ModuleId,
                CreatedBy = workflowDetails.CreatedBy,
                CreatedDate = workflowDetails.CreatedDate
            };
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database error while recording property visit. " +
                "PropertyId={PropertyId}, " +
                "WorkflowStageId={WorkflowStageId}, " +
                "ModuleId={ModuleId}, UserId={UserId}",
                request.PropertyId,
                request.WorkflowStageId,
                request.ModuleId,
                loggedInUserId);

            throw new InvalidOperationException(
                "Unable to record the property visit.",
                exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error while recording property visit. " +
                "PropertyId={PropertyId}, " +
                "WorkflowStageId={WorkflowStageId}, " +
                "ModuleId={ModuleId}, UserId={UserId}",
                request.PropertyId,
                request.WorkflowStageId,
                request.ModuleId,
                loggedInUserId);

            throw;
        }
    }

    public async Task<PropertyVisitTrackerResponseDto>
        GetVisitsAsync(
            PropertyVisitTrackerQueryParameters queryParameters,
            int loggedInUserId,
            string? loggedInRole,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Logged-in user information is invalid.");
        }

        ValidateDateRange(queryParameters);

        var pageNumber = queryParameters.PageNumber <= 0
            ? 1
            : queryParameters.PageNumber;

        var pageSize = queryParameters.PageSize <= 0
            ? 20
            : Math.Min(queryParameters.PageSize, 100);

        var query =
            from workflow in _workflowDetailsRepository
                .GetQueryable()
                .AsNoTracking()

            join stage in _workflowStageRepository
                .GetQueryable()
                .AsNoTracking()
                on workflow.WorkflowStageId equals stage.Id

            join property in _repository
                .GetQueryable()
                .AsNoTracking()
                on workflow.PropertyId equals property.Id

            join ward in _wardRepository
                .GetQueryable()
                .AsNoTracking()
                on property.WardId equals ward.Id
                into wardGroup

            from ward in wardGroup.DefaultIfEmpty()

            join user in _userRepository
                .GetQueryable()
                .AsNoTracking()
                on workflow.CreatedBy equals user.Id
                into userGroup

            from user in userGroup.DefaultIfEmpty()

            join surveyVisit in _propertySurveyVisitRepository
                .GetQueryable()
                .AsNoTracking()
                on workflow.Id equals surveyVisit.PropertyWorkflowDetailsId
                into surveyVisitGroup

            from surveyVisit in surveyVisitGroup
                .OrderByDescending(x => x.Id)
                .Take(1)
                .DefaultIfEmpty()

            where workflow.IsActive
                  && stage.IsActive
                  && property.IsActive
                  && !property.MarkedForDeletion

            select new
            {
                Workflow = workflow,
                Stage = stage,
                Property = property,
                Ward = ward,
                User = user,
                SurveyVisit = surveyVisit
            };

        /*
         * Surveyor can view only their own visits.
         */
        if (IsSurveyor(loggedInRole))
        {
            query = query.Where(x =>
                x.Workflow.CreatedBy == loggedInUserId);
        }

        /*
         * Optional user filter.
         */
        if (queryParameters.UserId.HasValue)
        {
            if (IsSurveyor(loggedInRole) &&
                queryParameters.UserId.Value != loggedInUserId)
            {
                throw new UnauthorizedAccessException(
                    "Surveyors can view only their own property visits.");
            }

            query = query.Where(x =>
                x.Workflow.CreatedBy ==
                queryParameters.UserId.Value);
        }

        if (queryParameters.PropertyId.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.PropertyId ==
                queryParameters.PropertyId.Value);
        }

        if (queryParameters.WorkflowStageId.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.WorkflowStageId ==
                queryParameters.WorkflowStageId.Value);
        }

        if (queryParameters.ModuleId.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.ModuleId ==
                queryParameters.ModuleId.Value);
        }

        if (queryParameters.IsActive.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.IsActive ==
                queryParameters.IsActive.Value);
        }

        if (queryParameters.FromDateTime.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.CreatedDate >=
                queryParameters.FromDateTime.Value);
        }

        if (queryParameters.ToDateTime.HasValue)
        {
            query = query.Where(x =>
                x.Workflow.CreatedDate <=
                queryParameters.ToDateTime.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                queryParameters.WardNo))
        {
            var wardNo =
                queryParameters.WardNo.Trim();

            query = query.Where(x =>
                x.Ward != null &&
                x.Ward.WardNo != null &&
                x.Ward.WardNo == wardNo);
        }

        if (!string.IsNullOrWhiteSpace(
                queryParameters.PropertyNo))
        {
            var propertyNo =
                queryParameters.PropertyNo.Trim();

            query = query.Where(x =>
                x.Property.PropertyNo != null &&
                x.Property.PropertyNo.Contains(propertyNo));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);

        var skip = checked(
            (pageNumber - 1) * pageSize);

        var visitList = await query
            .OrderByDescending(x =>
                x.Workflow.CreatedDate)
            .ThenByDescending(x =>
                x.Workflow.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new PropertyVisitTrackerListDto
            {
                VisitId =
                    x.Workflow.Id,

                PropertyId =
                    x.Workflow.PropertyId,

                WardNo =
                    x.Ward != null
                        ? x.Ward.WardNo
                        : null,

                PropertyNo =
                    x.Property.PropertyNo,

                PartitionNo =
                    x.Property.PartitionNo,

                DisplayPropertyNo =
                    (x.Property.PropertyNo ?? string.Empty) +
                    (
                        string.IsNullOrWhiteSpace(
                            x.Property.PartitionNo)
                            ? string.Empty
                            : "-" + x.Property.PartitionNo
                    ),

                WorkflowStageId =
                    x.Workflow.WorkflowStageId,

                WorkflowStageName =
                    x.Stage.StageName,

                WorkflowStageDescription =
                    x.Stage.Description,

                ModuleId =
                    x.Workflow.ModuleId,

                UserId =
                    x.Workflow.CreatedBy,

                UserName =
                    x.User != null
                        ? x.User.UserName
                        : null,

                VisitDateTime =
                    x.Workflow.CreatedDate,

                IsActive =
                    x.Workflow.IsActive,

                Latitude =
                    x.SurveyVisit != null
                        ? x.SurveyVisit.Latitude
                        : null,

                Longitude =
                    x.SurveyVisit != null
                        ? x.SurveyVisit.Longitude
                        : null,

                Location =
                    x.SurveyVisit != null
                        ? x.SurveyVisit.Location
                        : null
            })
            .ToListAsync(cancellationToken);

        return new PropertyVisitTrackerResponseDto
        {
            Status = true,
            Message = visitList.Count > 0
                ? "Property visits fetched successfully."
                : "No property visits found.",
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = pageNumber < totalPages,
            HasPrevious = pageNumber > 1,
            VisitList = visitList
        };
    }

    private static void ValidateDateRange(
        PropertyVisitTrackerQueryParameters queryParameters)
    {
        if (queryParameters.FromDateTime.HasValue &&
            queryParameters.ToDateTime.HasValue &&
            queryParameters.FromDateTime.Value >
            queryParameters.ToDateTime.Value)
        {
            throw new ArgumentException(
                "FromDateTime cannot be greater than ToDateTime.");
        }
    }

    private static bool IsSurveyor(string? role)
    {
        return string.Equals(
            role?.Trim(),
            "SURVEYOR",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CreatePropertySurveyVisitResponseDto>
        CreateSurveyVisitAsync(
            CreatePropertySurveyVisitDto request,
            int loggedInUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Logged-in user information is invalid.");
        }

        var propertyExists = await _repository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == request.PropertyId &&
                    x.IsActive &&
                    !x.MarkedForDeletion,
                cancellationToken);

        if (!propertyExists)
        {
            throw new KeyNotFoundException(
                $"Property with ID {request.PropertyId} was not found.");
        }

        var workflowStageExists = await _workflowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == request.WorkflowStageId &&
                    x.IsActive,
                cancellationToken);

        if (!workflowStageExists)
        {
            throw new ArgumentException(
                $"Invalid or inactive WorkflowStageId: " +
                $"{request.WorkflowStageId}.");
        }

        if (request.RemarkId.HasValue && _commonRemarkDetailsRepository != null)
        {
            var remarkExists = await _commonRemarkDetailsRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == request.RemarkId.Value &&
                        x.IsActive,
                    cancellationToken);

            if (!remarkExists)
            {
                throw new ArgumentException(
                    $"Invalid or inactive RemarkId: " +
                    $"{request.RemarkId.Value}.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var createdDate = DateTime.Now;

            // STEP 1: Insert PTIS.PropertyWorkflowDetails
            var workflowDetails =
                new PropertyWorkflowDetailsEntity
                {
                    PropertyId = request.PropertyId,
                    WorkflowStageId = request.WorkflowStageId,
                    ModuleId = request.ModuleId,
                    IsActive = true,
                    CreatedBy = loggedInUserId,
                    CreatedDate = createdDate,
                    UpdatedBy = null,
                    UpdatedDate = null
                };

            await _workflowDetailsRepository.AddAsync(
                workflowDetails,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            // STEP 2: Insert GSMS.PropertySurveyVisit
            var surveyVisit =
                new PropertySurveyVisitEntity
                {
                    PropertyWorkflowDetailsId = workflowDetails.Id,
                    InternalSurveyVerified = request.InternalSurveyVerified,
                    RemarkId = request.RemarkId,
                    RemarkText = string.IsNullOrWhiteSpace(request.RemarkText)
                        ? null
                        : request.RemarkText.Trim(),
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Location = string.IsNullOrWhiteSpace(request.Location)
                        ? null
                        : request.Location.Trim(),
                    IsActive = true,
                    CreatedBy = loggedInUserId,
                    CreatedDate = createdDate,
                    UpdatedBy = null,
                    UpdatedDate = null
                };

            await _propertySurveyVisitRepository.AddAsync(
                surveyVisit,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new CreatePropertySurveyVisitResponseDto
            {
                Status = true,
                Message = "Property survey visit recorded successfully.",
                PropertyId = workflowDetails.PropertyId,
                PropertyWorkflowDetailsId = workflowDetails.Id,
                SurveyVisitId = surveyVisit.Id,
                WorkflowStageId = workflowDetails.WorkflowStageId,
                ModuleId = workflowDetails.ModuleId,
                InternalSurveyVerified = surveyVisit.InternalSurveyVerified ?? false,
                RemarkId = surveyVisit.RemarkId,
                RemarkText = surveyVisit.RemarkText,
                Latitude = surveyVisit.Latitude,
                Longitude = surveyVisit.Longitude,
                Location = surveyVisit.Location,
                CreatedBy = surveyVisit.CreatedBy
            };
        }
        catch (DbUpdateException exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(
                exception,
                "Database error while recording property survey visit. " +
                "PropertyId={PropertyId}, " +
                "PropertyWorkflowDetailsId={PropertyWorkflowDetailsId}, " +
                "RemarkId={RemarkId}, " +
                "UserId={UserId}",
                request.PropertyId,
                request.WorkflowStageId,
                request.RemarkId,
                loggedInUserId);

            throw new InvalidOperationException(
                "Unable to record the property survey visit.",
                exception);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VerifyPropertySurveyVisitResponseDto> VerifyPropertySurveyVisitAsync(
        VerifyPropertySurveyVisitDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException("Logged-in user information is invalid.");
        }

        if ((!request.PropertyId.HasValue || request.PropertyId.Value <= 0) &&
            (!request.WingDetailId.HasValue || request.WingDetailId.Value <= 0))
        {
            throw new ArgumentException("Please provide either PropertyId or WingDetailId for verification.");
        }

        var workflowStageExists = await _workflowStageRepository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id == request.WorkflowStageId &&
                    x.IsActive,
                cancellationToken);

        if (!workflowStageExists)
        {
            throw new ArgumentException(
                $"Invalid or inactive WorkflowStageId: {request.WorkflowStageId}.");
        }

        if (request.RemarkId.HasValue && _commonRemarkDetailsRepository != null)
        {
            var remarkExists = await _commonRemarkDetailsRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == request.RemarkId.Value &&
                        x.IsActive,
                    cancellationToken);

            if (!remarkExists)
            {
                throw new ArgumentException(
                    $"Invalid or inactive RemarkId: {request.RemarkId.Value}.");
            }
        }

        var currentDate = DateTime.Now;
        int? targetSocietyDetailId = null;
        string? targetSocietyName = null;
        bool isMainSocietyWithoutWings = false;
        int? requestOrPropertyWingDetailId = request.WingDetailId;
        int resolvedPropertyId = request.PropertyId ?? 0;
        var propertyIdsToVerify = new List<int>();
        var verifiedPropertiesList = new List<PropertyVerificationStatusItemDto>();
        var unverifiedPropertiesList = new List<PropertyVerificationStatusItemDto>();

        var wingDetailsRepo = _wingDetailsMastRepository;

        // CASE 1: Direct Wing Verification (WingDetailId is provided)
        if (request.WingDetailId.HasValue && request.WingDetailId.Value > 0)
        {
            if (wingDetailsRepo == null)
            {
                throw new InvalidOperationException("WingDetails repository is not configured.");
            }

            var wing = await wingDetailsRepo.GetQueryable().AsNoTracking()
                .Where(x => x.Id == request.WingDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.WingName,
                    x.SocietyDetailsMastId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (wing == null)
            {
                throw new ArgumentException(
                    $"Wing with ID {request.WingDetailId.Value} not found.");
            }

            targetSocietyDetailId = wing.SocietyDetailsMastId;

            // Resolve parent PropertyId from SocietyDetailsMast if not already passed
            if (resolvedPropertyId <= 0 && targetSocietyDetailId.HasValue && targetSocietyDetailId.Value > 0)
            {
                var society = await _societyRepository.GetQueryable().AsNoTracking()
                    .Where(x => x.Id == targetSocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                    .Select(x => new { x.Id, x.PropertyId, x.SocietyName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (society?.PropertyId != null && society.PropertyId > 0)
                {
                    resolvedPropertyId = society.PropertyId.Value;
                    targetSocietyName = society.SocietyName;
                }
            }

            // Validate Wing photo (EntityType = 'W', PropertyId IS NULL or 0)
            var hasWingPhoto = await _propertyPhotoRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WingDetailId == wing.Id &&
                    (x.PropertyId == null || x.PropertyId == 0) &&
                    x.EntityType == "W" &&
                    x.IsActive &&
                    !x.MarkedForDeletion &&
                    x.IsLatest,
                    cancellationToken);

            if (!hasWingPhoto)
            {
                throw new ArgumentException(
                    $"Please capture wing photo for {wing.WingName ?? "Wing"} before verification.");
            }

            // Fetch all child properties under this wing
            var wingProperties = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.WingDetailId == wing.Id &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    PropertyNo = !string.IsNullOrWhiteSpace(x.PropertyNo) ? x.PropertyNo : x.Id.ToString(),
                    PartitionNo = !string.IsNullOrWhiteSpace(x.PartitionNo) ? x.PartitionNo : "N/A"
                })
                .ToListAsync(cancellationToken);

            if (wingProperties.Count > 0)
            {
                var wingPropIds = wingProperties.Select(w => w.Id).ToList();

                var propertiesWithPhoto = await _propertyPhotoRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        wingPropIds.Contains(x.PropertyId ?? 0) &&
                        x.IsActive &&
                        !x.MarkedForDeletion &&
                        x.IsLatest)
                    .Select(x => x.PropertyId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var prop in wingProperties)
                {
                    var hasPPhoto = propertiesWithPhoto.Contains(prop.Id);
                    var item = new PropertyVerificationStatusItemDto
                    {
                        PropertyId = prop.Id,
                        PropertyNo = prop.PropertyNo,
                        PartitionNo = prop.PartitionNo,
                        HasPhoto = hasPPhoto,
                        IsVerified = hasPPhoto,
                        StatusMessage = hasPPhoto ? "Verified successfully" : "Photo not available"
                    };

                    if (hasPPhoto)
                    {
                        verifiedPropertiesList.Add(item);
                        propertyIdsToVerify.Add(prop.Id);
                    }
                    else
                    {
                        unverifiedPropertiesList.Add(item);
                    }
                }
            }
        }
        // CASE 2: Property Verification (PropertyId is provided without WingDetailId)
        else
        {
            var propertyDetails = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == request.PropertyId!.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
               .Select(x => new
               {
                   x.Id,
                   x.WingDetailId,
                   x.PartitionNo,
                   x.WardId,
                   x.PropertyNo
               })
                .FirstOrDefaultAsync(cancellationToken);

            if (propertyDetails == null)
            {
                throw new KeyNotFoundException(
                    "Property not found.");
            }

            resolvedPropertyId = propertyDetails.Id;

            // Resolve target Society ID directly from SocietyDetailsMast using PropertyId
            var societyInfo = await _societyRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.PropertyId == propertyDetails.Id &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new { x.Id, x.SocietyName })
                .FirstOrDefaultAsync(cancellationToken);

            if (societyInfo != null)
            {
                targetSocietyDetailId = societyInfo.Id;
                targetSocietyName = societyInfo.SocietyName;
            }

            // If not found directly, check if this property has a WingDetailId pointing to a Society
            if (!targetSocietyDetailId.HasValue && propertyDetails.WingDetailId.HasValue && propertyDetails.WingDetailId.Value > 0 && wingDetailsRepo != null)
            {
                var wingInfo = await wingDetailsRepo
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == propertyDetails.WingDetailId.Value &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => new { x.SocietyDetailsMastId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (wingInfo?.SocietyDetailsMastId != null)
                {
                    targetSocietyDetailId = wingInfo.SocietyDetailsMastId;
                    targetSocietyName = await _societyRepository
                        .GetQueryable()
                        .AsNoTracking()
                        .Where(x => x.Id == targetSocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                        .Select(x => x.SocietyName)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }

            requestOrPropertyWingDetailId = propertyDetails.WingDetailId;

            // Standalone individual property (not belonging to a Society/Wing)
            if (!targetSocietyDetailId.HasValue || targetSocietyDetailId.Value <= 0)
            {
                var hasPhoto = await _propertyPhotoRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.PropertyId == request.PropertyId &&
                            x.IsActive &&
                            !x.MarkedForDeletion &&
                            x.IsLatest,
                        cancellationToken);

                if (!hasPhoto)
                {
                    var propNoStr = !string.IsNullOrWhiteSpace(propertyDetails.PropertyNo) ? propertyDetails.PropertyNo : request.PropertyId.ToString();
                    var partNoStr = !string.IsNullOrWhiteSpace(propertyDetails.PartitionNo) ? propertyDetails.PartitionNo : "N/A";

                    throw new ArgumentException(
                        $"Please click photo for Property No: {propNoStr}, Partition No: {partNoStr} before property verification.");
                }

                propertyIdsToVerify.Add(request.PropertyId!.Value);
                verifiedPropertiesList.Add(new PropertyVerificationStatusItemDto
                {
                    PropertyId = propertyDetails.Id,
                    PropertyNo = !string.IsNullOrWhiteSpace(propertyDetails.PropertyNo) ? propertyDetails.PropertyNo : propertyDetails.Id.ToString(),
                    PartitionNo = !string.IsNullOrWhiteSpace(propertyDetails.PartitionNo) ? propertyDetails.PartitionNo : "N/A",
                    HasPhoto = true,
                    IsVerified = true,
                    StatusMessage = "Verified successfully"
                });
            }
            else
            {
                // Society & Wing photo validation
                var societyWingsQuery = wingDetailsRepo != null
                    ? wingDetailsRepo
                        .GetQueryable()
                        .AsNoTracking()
                        .Where(x =>
                            x.SocietyDetailsMastId == targetSocietyDetailId.Value &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                    : Enumerable.Empty<WingDetailsMastEntity>().AsQueryable();

                if (requestOrPropertyWingDetailId.HasValue && requestOrPropertyWingDetailId.Value > 0)
                {
                    societyWingsQuery = societyWingsQuery.Where(x => x.Id == requestOrPropertyWingDetailId.Value);
                }

                var societyWings = await societyWingsQuery
                    .Select(x => new
                    {
                        x.Id,
                        x.WingName
                    })
                    .ToListAsync(cancellationToken);

                if (societyWings.Count > 0)
                {
                    // If society has only ONE wing, enforce that the single wing must have a wing photo
                    if (societyWings.Count == 1)
                    {
                        var singleWing = societyWings.First();
                        var hasSingleWingPhoto = await _propertyPhotoRepository
                            .GetQueryable()
                            .AsNoTracking()
                            .AnyAsync(x =>
                                x.WingDetailId == singleWing.Id &&
                                (x.PropertyId == null || x.PropertyId == 0) &&
                                x.EntityType == "W" &&
                                x.IsActive &&
                                !x.MarkedForDeletion &&
                                x.IsLatest,
                                cancellationToken);

                        if (!hasSingleWingPhoto)
                        {
                            throw new ArgumentException(
                                $"Please capture wing photo for {singleWing.WingName ?? "Wing"} before verification.");
                        }
                    }

                    var allSocietyWingPropertyIdsToVerify = new List<int>();

                    foreach (var wing in societyWings)
                    {
                        // Fetch all properties under this wing
                        var wingProperties = await _repository
                            .GetQueryable()
                            .AsNoTracking()
                            .Where(x =>
                                x.WingDetailId == wing.Id &&
                                x.IsActive &&
                                !x.MarkedForDeletion)
                            .Select(x => new
                            {
                                x.Id,
                                PropertyNo = !string.IsNullOrWhiteSpace(x.PropertyNo) ? x.PropertyNo : x.Id.ToString(),
                                PartitionNo = !string.IsNullOrWhiteSpace(x.PartitionNo) ? x.PartitionNo : "N/A"
                            })
                            .ToListAsync(cancellationToken);

                        if (wingProperties.Count > 0)
                        {
                            var wingPropIds = wingProperties.Select(w => w.Id).ToList();

                            var propertiesWithPhoto = await _propertyPhotoRepository
                                .GetQueryable()
                                .AsNoTracking()
                                .Where(x =>
                                    wingPropIds.Contains(x.PropertyId ?? 0) &&
                                    x.IsActive &&
                                    !x.MarkedForDeletion &&
                                    x.IsLatest)
                                .Select(x => x.PropertyId)
                                .Where(id => id.HasValue)
                                .Select(id => id!.Value)
                                .Distinct()
                                .ToListAsync(cancellationToken);

                            foreach (var prop in wingProperties)
                            {
                                var hasPPhoto = propertiesWithPhoto.Contains(prop.Id);
                                var item = new PropertyVerificationStatusItemDto
                                {
                                    PropertyId = prop.Id,
                                    PropertyNo = prop.PropertyNo,
                                    PartitionNo = prop.PartitionNo,
                                    HasPhoto = hasPPhoto,
                                    IsVerified = hasPPhoto,
                                    StatusMessage = hasPPhoto ? "Verified successfully" : "Photo not available"
                                };

                                if (hasPPhoto)
                                {
                                    verifiedPropertiesList.Add(item);
                                    allSocietyWingPropertyIdsToVerify.Add(prop.Id);
                                }
                                else
                                {
                                    unverifiedPropertiesList.Add(item);
                                }
                            }
                        }
                    }

                    propertyIdsToVerify = allSocietyWingPropertyIdsToVerify
                        .Distinct()
                        .ToList();
                }
                else
                {
                    // Society has NO wings: verify the main society property directly using its photo
                    isMainSocietyWithoutWings = true;

                    var hasPhoto = await _propertyPhotoRepository
                        .GetQueryable()
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.PropertyId == request.PropertyId &&
                                x.IsActive &&
                                !x.MarkedForDeletion &&
                                x.IsLatest,
                            cancellationToken);

                    var propNoStr = !string.IsNullOrWhiteSpace(propertyDetails.PropertyNo) ? propertyDetails.PropertyNo : request.PropertyId.ToString();
                    var partNoStr = !string.IsNullOrWhiteSpace(propertyDetails.PartitionNo) ? propertyDetails.PartitionNo : "N/A";
                    var societyLabel = !string.IsNullOrWhiteSpace(targetSocietyName) ? $" '{targetSocietyName}'" : "";

                    if (!hasPhoto)
                    {
                        throw new ArgumentException(
                            $"This is Main Society{societyLabel} and it has no wings. Please click photo for Property No: {propNoStr}, Partition No: {partNoStr} before property verification.");
                    }

                    propertyIdsToVerify.Add(request.PropertyId!.Value);
                    verifiedPropertiesList.Add(new PropertyVerificationStatusItemDto
                    {
                        PropertyId = propertyDetails.Id,
                        PropertyNo = propNoStr,
                        PartitionNo = partNoStr,
                        HasPhoto = true,
                        IsVerified = true,
                        StatusMessage = $"This is Main Society{societyLabel} and it has no wings. Verified successfully."
                    });
                }
            }
        }

        int requestedWorkflowDetailsId = 0;
        int requestedSurveyVisitId = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var propertyId in propertyIdsToVerify)
            {
                // Deactivate existing active workflows for this property
                var existingActiveWorkflows = await _workflowDetailsRepository
                    .GetQueryable()
                    .Where(x =>
                        x.PropertyId == propertyId &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var existingWorkflow in existingActiveWorkflows)
                {
                    existingWorkflow.IsActive = false;
                    existingWorkflow.UpdatedBy = loggedInUserId;
                    existingWorkflow.UpdatedDate = currentDate;

                    var existingSurveyVisits = await _propertySurveyVisitRepository
                        .GetQueryable()
                        .Where(x =>
                            x.PropertyWorkflowDetailsId == existingWorkflow.Id &&
                            x.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var existingVisit in existingSurveyVisits)
                    {
                        existingVisit.IsActive = false;
                        existingVisit.UpdatedBy = loggedInUserId;
                        existingVisit.UpdatedDate = currentDate;
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create new workflow
                var workflowDetails = new PropertyWorkflowDetailsEntity
                {
                    PropertyId = propertyId,
                    WorkflowStageId = request.WorkflowStageId,
                    ModuleId = request.ModuleId,
                    IsActive = true,
                    CreatedBy = loggedInUserId,
                    CreatedDate = currentDate
                };

                await _workflowDetailsRepository.AddAsync(
                    workflowDetails,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create new verified survey visit
                var surveyVisit = new PropertySurveyVisitEntity
                {
                    PropertyWorkflowDetailsId = workflowDetails.Id,
                    InternalSurveyVerified = true,

                    RemarkId = request.RemarkId,
                    RemarkText = request.RemarkText,

                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Location = request.Location,

                    IsActive = true,

                    CreatedBy = loggedInUserId,
                    CreatedDate = currentDate
                };

                await _propertySurveyVisitRepository.AddAsync(
                    surveyVisit,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Keep response IDs for originally requested or resolved property
                if (propertyId == resolvedPropertyId || requestedWorkflowDetailsId == 0)
                {
                    requestedWorkflowDetailsId = workflowDetails.Id;
                    requestedSurveyVisitId = surveyVisit.Id;
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new VerifyPropertySurveyVisitResponseDto
        {
            Status = true,
            Message = unverifiedPropertiesList.Count > 0
                ? $"Property verification completed. {verifiedPropertiesList.Count} properties verified, {unverifiedPropertiesList.Count} properties skipped (missing photo)."
                : (propertyIdsToVerify.Count > 0 
                    ? (isMainSocietyWithoutWings && !string.IsNullOrWhiteSpace(targetSocietyName) 
                        ? $"This is Main Society '{targetSocietyName}' and it has no wings. Property verified successfully." 
                        : "Property verified successfully.") 
                    : "No properties verified."),
            PropertyId = resolvedPropertyId > 0 ? resolvedPropertyId : request.PropertyId,
            WingDetailId = requestOrPropertyWingDetailId,
            PropertyWorkflowDetailsId = requestedWorkflowDetailsId,
            SurveyVisitId = requestedSurveyVisitId,
            IsVerified = propertyIdsToVerify.Count > 0,
            VerifiedProperties = verifiedPropertiesList,
            UnverifiedProperties = unverifiedPropertiesList
        };
    }

    public async Task<UnverifyPropertySurveyVisitResponseDto> UnverifyPropertySurveyVisitAsync(
        UnverifyPropertySurveyVisitDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Logged-in user information is invalid.");
        }

        if ((!request.PropertyId.HasValue || request.PropertyId.Value <= 0) &&
            (!request.WingDetailId.HasValue || request.WingDetailId.Value <= 0))
        {
            throw new ArgumentException("Please provide either PropertyId or WingDetailId for unverification.");
        }

        var currentDate = DateTime.Now;
        var propertyIdsToUnverify = new List<int>();

        // CASE 1: Direct Wing Unverification (WingDetailId is provided)
        if (request.WingDetailId.HasValue && request.WingDetailId.Value > 0)
        {
            var wingPropertyIds = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.WingDetailId == request.WingDetailId.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            propertyIdsToUnverify.AddRange(wingPropertyIds);

            if (request.PropertyId.HasValue && request.PropertyId.Value > 0)
            {
                if (!propertyIdsToUnverify.Contains(request.PropertyId.Value))
                {
                    propertyIdsToUnverify.Add(request.PropertyId.Value);
                }
            }
        }
        // CASE 2: Property Unverification (PropertyId is provided without WingDetailId)
        else if (request.PropertyId.HasValue && request.PropertyId.Value > 0)
        {
            var propertyDetails = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == request.PropertyId.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.WingDetailId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (propertyDetails == null)
            {
                throw new KeyNotFoundException(
                    "Property not found.");
            }

            propertyIdsToUnverify.Add(propertyDetails.Id);

            if (propertyDetails.WingDetailId.HasValue)
            {
                var wingPropertyIds = await _repository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.WingDetailId == propertyDetails.WingDetailId.Value &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                propertyIdsToUnverify = wingPropertyIds
                    .Append(propertyDetails.Id)
                    .Distinct()
                    .ToList();
            }
        }

        if (propertyIdsToUnverify.Count == 0)
        {
            throw new KeyNotFoundException("No active properties found for unverification.");
        }

        var propertiesToUnverifyDetails = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x => propertyIdsToUnverify.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                PropertyNo = !string.IsNullOrWhiteSpace(x.PropertyNo) ? x.PropertyNo : x.Id.ToString(),
                PartitionNo = !string.IsNullOrWhiteSpace(x.PartitionNo) ? x.PartitionNo : "N/A"
            })
            .ToListAsync(cancellationToken);

        var unverifiedPropertiesList = new List<PropertyVerificationStatusItemDto>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var propertyId in propertyIdsToUnverify)
            {
                // 1. Find currently active workflow for this property.
                var workflowDetails = await _workflowDetailsRepository
                    .GetQueryable()
                    .Where(x =>
                        x.PropertyId == propertyId &&
                        x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                // If a Wing member has no active workflow,
                // simply continue with the remaining properties.
                if (workflowDetails == null)
                {
                    continue;
                }

                // 2. Find current active survey visit.
                var existingSurveyVisit = await _propertySurveyVisitRepository
                    .GetQueryable()
                    .Where(x =>
                        x.PropertyWorkflowDetailsId == workflowDetails.Id &&
                        x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                // 3. Deactivate active workflow (IsActive = 0 in PTIS.PropertyWorkFlowDetails)
                workflowDetails.IsActive = false;
                workflowDetails.UpdatedBy = loggedInUserId;
                workflowDetails.UpdatedDate = currentDate;

                if (existingSurveyVisit != null)
                {
                    // 4. Deactivate survey visit & set InternalSurveyVerified = 0 and IsActive = 0 in GSMS.PropertySurveyVisit
                    existingSurveyVisit.InternalSurveyVerified = false;
                    existingSurveyVisit.IsActive = false;
                    existingSurveyVisit.UpdatedBy = loggedInUserId;
                    existingSurveyVisit.UpdatedDate = currentDate;
                }

                // 5. Create NEW UnVerified history row.
                var unverifiedSurveyVisit = new PropertySurveyVisitEntity
                {
                    PropertyWorkflowDetailsId = workflowDetails.Id,
                    InternalSurveyVerified = false,
                    RemarkId = request.RemarkId,
                    RemarkText = request.RemarkText,
                    Latitude = existingSurveyVisit?.Latitude,
                    Longitude = existingSurveyVisit?.Longitude,
                    Location = existingSurveyVisit?.Location,
                    IsActive = false,
                    CreatedBy = loggedInUserId,
                    CreatedDate = currentDate
                };

                await _propertySurveyVisitRepository.AddAsync(
                    unverifiedSurveyVisit,
                    cancellationToken);

                var propInfo = propertiesToUnverifyDetails.FirstOrDefault(p => p.Id == propertyId);
                unverifiedPropertiesList.Add(new PropertyVerificationStatusItemDto
                {
                    PropertyId = propertyId,
                    PropertyNo = propInfo?.PropertyNo ?? propertyId.ToString(),
                    PartitionNo = propInfo?.PartitionNo ?? "N/A",
                    IsVerified = false,
                    HasPhoto = true,
                    StatusMessage = "Unverified successfully"
                });
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new UnverifyPropertySurveyVisitResponseDto
        {
            Status = true,
            Message = unverifiedPropertiesList.Count > 0
                ? $"Property unverification completed. {unverifiedPropertiesList.Count} properties unverified."
                : "No properties unverified.",
            PropertyId = request.PropertyId,
            WingDetailId = request.WingDetailId,
            IsVerified = false,
            UnverifiedProperties = unverifiedPropertiesList
        };
    }
}
