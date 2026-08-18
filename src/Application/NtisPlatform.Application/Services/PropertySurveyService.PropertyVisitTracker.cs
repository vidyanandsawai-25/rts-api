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

        if (request.RemarkId.HasValue)
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
                InternalSurveyVerified = surveyVisit.InternalSurveyVerified,
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

    public async Task<VerifyPropertySurveyVisitResponseDto>
        VerifyPropertySurveyVisitAsync(
            VerifyPropertySurveyVisitDto request,
            int loggedInUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (loggedInUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Logged-in user information is invalid.");
        }

        // 1. Check property exists
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
                "Property not found.");
        }

        // 1b. Check workflow stage exists and is active
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

        // 2. Property must have photo before verification
        var hasPhoto = await _propertyPhotoRepository
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.PropertyId == request.PropertyId &&
                    x.IsActive &&
                    !x.MarkedForDeletion,
                cancellationToken);

        if (!hasPhoto)
        {
            throw new PropertyValidationException(
                "Please click photo before property verification.");
    
        }

        var currentDate = DateTime.Now;

        var propertyDetails = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.Id == request.PropertyId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .Select(x => new
            {
                x.Id,
                x.SocietyDetailId,
                x.PartitionNo,
                x.WardId,
                x.PropertyNo
            })
            .FirstOrDefaultAsync(cancellationToken);

        var propertyIdsToVerify = new List<int>
        {
            request.PropertyId
        };
        var isSocietyOrWingHandled = false;
        if (propertyDetails?.SocietyDetailId != null)
        {
            var currentSocietyDetail = await _societyRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == propertyDetails.SocietyDetailId.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.PropertyId,
                    x.WingId,
                    x.WingName
                })
                .FirstOrDefaultAsync(cancellationToken);

            int? wingSocietyDetailId = null;
            string? wingName = null;

            if (currentSocietyDetail != null &&
                currentSocietyDetail.WingId == null &&
                string.IsNullOrWhiteSpace(propertyDetails.PartitionNo))
            {
                isSocietyOrWingHandled = true;
                var societyWingDetailIds = await _societyRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.PropertyId == currentSocietyDetail.PropertyId &&
                        x.WingId != null &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (societyWingDetailIds.Count > 0)
                {
                    var allWingPropertyIds = await _repository
                        .GetQueryable()
                        .AsNoTracking()
                        .Where(x =>
                            x.SocietyDetailId.HasValue &&
                            societyWingDetailIds.Contains(x.SocietyDetailId.Value) &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken);

                    var allWingPropertyIdsWithPhoto = await _propertyPhotoRepository
                        .GetQueryable()
                        .AsNoTracking()
                        .Where(x =>
                            allWingPropertyIds.Contains(x.PropertyId) &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                        .Select(x => x.PropertyId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    var allWingPropertyIdsWithoutPhoto = allWingPropertyIds
                        .Except(allWingPropertyIdsWithPhoto)
                        .ToList();

                    if (allWingPropertyIdsWithoutPhoto.Count > 0)
                    {
                        throw new ArgumentException(
                            "Please capture photo for all Wing properties " +
                            "before Main Society verification.");
                    }
                }
            }

            if (currentSocietyDetail?.WingId != null)
            {
                isSocietyOrWingHandled = true;
                wingSocietyDetailId = currentSocietyDetail.Id;
                wingName = currentSocietyDetail.WingName;
            }
            else if (currentSocietyDetail != null &&
                     currentSocietyDetail.WingId == null &&
                     !string.IsNullOrWhiteSpace(propertyDetails.PartitionNo))
            {
                var expectedWingName =
                    $"Wing {propertyDetails.PartitionNo.Trim()}";

                var wingDetails = await _societyRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.PropertyId == currentSocietyDetail.PropertyId &&
                        x.WingId != null &&
                        x.WingName == expectedWingName &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => new
                    {
                        x.Id,
                        x.WingName
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (wingDetails != null)
                {
                    isSocietyOrWingHandled = true;
                    wingSocietyDetailId = wingDetails.Id;
                    wingName = wingDetails.WingName;
                }
            }

            if (wingSocietyDetailId.HasValue)
            {
                var wingPropertyIds = await _repository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.SocietyDetailId == wingSocietyDetailId.Value &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                propertyIdsToVerify = wingPropertyIds
                    .Append(request.PropertyId)
                    .Distinct()
                    .ToList();

                var wingPropertyIdsWithPhoto = await _propertyPhotoRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        wingPropertyIds.Contains(x.PropertyId) &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => x.PropertyId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var wingPropertyIdsWithoutPhoto = wingPropertyIds
                    .Except(wingPropertyIdsWithPhoto)
                    .ToList();

                if (wingPropertyIdsWithoutPhoto.Count > 0)
                {
                    throw new ArgumentException(
                        $"Please capture photo for all properties of " +
                        $"{wingName} before verification.");
                }
            }
        }

        if (!isSocietyOrWingHandled &&
            propertyDetails != null &&
            !string.IsNullOrWhiteSpace(propertyDetails.PropertyNo))
        {
            var individualPropertyIds = await _repository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.WardId == propertyDetails.WardId &&
                    x.PropertyNo == propertyDetails.PropertyNo &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (individualPropertyIds.Count > 1)
            {
                var individualPropertyIdsWithPhoto =
                    await _propertyPhotoRepository
                        .GetQueryable()
                        .AsNoTracking()
                        .Where(x =>
                            individualPropertyIds.Contains(x.PropertyId) &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                        .Select(x => x.PropertyId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                var individualPropertyIdsWithoutPhoto =
                    individualPropertyIds
                        .Except(individualPropertyIdsWithPhoto)
                        .ToList();

                if (individualPropertyIdsWithoutPhoto.Count > 0)
                {
                    throw new ArgumentException(
                        "Please capture photo for all partition properties " +
                        "before verification.");
                }

                propertyIdsToVerify = individualPropertyIds
                    .Append(request.PropertyId)
                    .Distinct()
                    .ToList();
            }
        }

        int requestedWorkflowDetailsId = 0;
        int requestedSurveyVisitId = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingActiveWorkflows = await _workflowDetailsRepository
                .GetQueryable()
                .Where(x =>
                    propertyIdsToVerify.Contains(x.PropertyId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var existingWorkflowIds = existingActiveWorkflows.Select(w => w.Id).ToList();

            var existingSurveyVisits = await _propertySurveyVisitRepository
                .GetQueryable()
                .Where(x =>
                    existingWorkflowIds.Contains(x.PropertyWorkflowDetailsId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existingWorkflow in existingActiveWorkflows)
            {
                existingWorkflow.IsActive = false;
                existingWorkflow.UpdatedBy = loggedInUserId;
                existingWorkflow.UpdatedDate = currentDate;
            }

            foreach (var existingVisit in existingSurveyVisits)
            {
                existingVisit.IsActive = false;
                existingVisit.UpdatedBy = loggedInUserId;
                existingVisit.UpdatedDate = currentDate;
            }

            PropertyWorkflowDetailsEntity? requestedWorkflowDetails = null;
            PropertySurveyVisitEntity? requestedSurveyVisit = null;

            foreach (var propertyId in propertyIdsToVerify)
            {
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

                var surveyVisit = new PropertySurveyVisitEntity
                {
                    PropertyWorkflowDetails = workflowDetails,
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

                if (propertyId == request.PropertyId)
                {
                    requestedWorkflowDetails = workflowDetails;
                    requestedSurveyVisit = surveyVisit;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            requestedWorkflowDetailsId = requestedWorkflowDetails?.Id ?? 0;
            requestedSurveyVisitId = requestedSurveyVisit?.Id ?? 0;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new VerifyPropertySurveyVisitResponseDto
        {
            Status = true,
            Message = "Property verified successfully.",
            PropertyId = request.PropertyId,
            PropertyWorkflowDetailsId = requestedWorkflowDetailsId,
            SurveyVisitId = requestedSurveyVisitId,
            IsVerified = true
        };
    }

    public async Task<bool> UnverifyPropertySurveyVisitAsync(
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
        var currentDate = DateTime.Now;

        var propertyDetails = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.Id == request.PropertyId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .Select(x => new
            {
                x.Id,
                x.SocietyDetailId,
                x.PartitionNo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (propertyDetails == null)
        {
            throw new KeyNotFoundException(
                "Property not found.");
        }

        var propertyIdsToUnverify = new List<int>
        {
            request.PropertyId
        };

        if (propertyDetails.SocietyDetailId.HasValue)
        {
            var currentSocietyDetail = await _societyRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == propertyDetails.SocietyDetailId.Value &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.PropertyId,
                    x.WingId,
                    x.WingName
                })
                .FirstOrDefaultAsync(cancellationToken);

            int? wingSocietyDetailId = null;

            if (currentSocietyDetail?.WingId != null)
            {
                wingSocietyDetailId = currentSocietyDetail.Id;
            }
            else if (currentSocietyDetail != null &&
                     currentSocietyDetail.WingId == null &&
                     !string.IsNullOrWhiteSpace(propertyDetails.PartitionNo))
            {
                var expectedWingName =
                    $"Wing {propertyDetails.PartitionNo.Trim()}";

                var wingDetails = await _societyRepository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.PropertyId == currentSocietyDetail.PropertyId &&
                        x.WingId != null &&
                        x.WingName == expectedWingName &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => new
                    {
                        x.Id
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (wingDetails != null)
                {
                    wingSocietyDetailId = wingDetails.Id;
                }
            }

            if (wingSocietyDetailId.HasValue)
            {
                var wingPropertyIds = await _repository
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.SocietyDetailId == wingSocietyDetailId.Value &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                propertyIdsToUnverify = wingPropertyIds
                    .Append(request.PropertyId)
                    .Distinct()
                    .ToList();
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var propertyId in propertyIdsToUnverify)
            {
                var workflowDetails = await _workflowDetailsRepository
                    .GetQueryable()
                    .Where(x =>
                        x.PropertyId == propertyId &&
                        x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (workflowDetails == null)
                {
                    continue;
                }

                var existingSurveyVisit = await _propertySurveyVisitRepository
                    .GetQueryable()
                    .Where(x =>
                        x.PropertyWorkflowDetailsId == workflowDetails.Id &&
                        x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingSurveyVisit == null)
                {
                    continue;
                }

                workflowDetails.IsActive = false;
                workflowDetails.UpdatedBy = loggedInUserId;
                workflowDetails.UpdatedDate = currentDate;

                existingSurveyVisit.IsActive = false;
                existingSurveyVisit.UpdatedBy = loggedInUserId;
                existingSurveyVisit.UpdatedDate = currentDate;

                var unverifiedSurveyVisit = new PropertySurveyVisitEntity
                {
                    PropertyWorkflowDetailsId = workflowDetails.Id,
                    InternalSurveyVerified = false,
                    RemarkId = request.RemarkId,
                    RemarkText = request.RemarkText,
                    Latitude = existingSurveyVisit.Latitude,
                    Longitude = existingSurveyVisit.Longitude,
                    Location = existingSurveyVisit.Location,
                    IsActive = false,
                    CreatedBy = loggedInUserId,
                    CreatedDate = currentDate
                };

                await _propertySurveyVisitRepository.AddAsync(
                    unverifiedSurveyVisit,
                    cancellationToken);
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

        return true;
    }
}
