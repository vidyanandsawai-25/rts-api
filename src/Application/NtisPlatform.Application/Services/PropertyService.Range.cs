using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Property creation operations (Range, Single)
/// </summary>
public partial class PropertyService
{
    /// <summary>
    /// Creates multiple properties based on a range request with transactional consistency.
    /// All properties are created within a single transaction - if any fails, all are rolled back.
    /// </summary>
    /// <param name="request">The range creation parameters containing template and range bounds.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A RangeResult containing success/failure counts and any error messages.
    /// On success: SuccessCount equals total properties created, FailedCount is 0.
    /// On failure: SuccessCount is 0, FailedCount equals total range size, Errors contains details.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<RangeResult<CreateNewPropertyResponseDto>> CreatePropertiesFromRangeAsync(RangeCreateRequest<CreateNewPropertyDto> request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Template == null)
        {
            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: CreatePropertiesFromRange.Numeric.InitialSuccessCount,
                FailedCount: CreatePropertiesFromRange.Numeric.InitialFailedCount,
                Results: [],
                Errors: new List<string> { CreatePropertiesFromRange.Messages.TemplateCannotBeNull });
        }

        _logger.LogInformation("Starting property range creation: WardId={WardId}, RangeFrom={RangeFrom}, RangeTo={RangeTo}, Prefix={Prefix}, Suffix={Suffix}",request.Template.WardId, request.RangeFrom, request.RangeTo, request.Prefix, request.Suffix);
        var rangeValues = RangeGenerator.GenerateRangeValues(request.RangeFrom, request.RangeTo, request.Prefix, request.Suffix);

        _logger.LogInformation("Generated {Count} property numbers for range creation", rangeValues.Count);
        var results = new List<CreateNewPropertyResponseDto>();
        var errors = new List<string>();

        _logger.LogInformation("Beginning database transaction for property range creation");
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            for (int i = 0; i < rangeValues.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (string.IsNullOrWhiteSpace(rangeValues[i]))
                    {
                        _logger.LogWarning("Generated property name at index {Index} is empty or null", i);
                        errors.Add($"{i + 1}: Generated property name is empty or null.");
                        break;
                    }

                    var propertyNo = rangeValues[i];
                    var sequenceNo = Convert.ToInt32(rangeValues[i]);

                    // Business Rule: Duplicate check (moved from repository to service)
                    var isDuplicate = await _propertyRepository.IsPropertyExists(request.Template.WardId,propertyNo,null);

                    if (isDuplicate)
                    {
                        _logger.LogWarning("Duplicate property detected: PropertyNo={PropertyNo}, WardId={WardId}. Checking remaining range.", propertyNo, request.Template.WardId);
                        
                        // Check remaining range for duplicates
                        for (int existCount = i; existCount < rangeValues.Count; existCount++)
                        {
                            if (await _propertyRepository.IsPropertyExists(request.Template.WardId,rangeValues[existCount],null))
                            {
                                errors.Add($"{rangeValues[existCount]} : {CreatePropertiesFromRange.Messages.PropertyAlreadyExists}");
                            }
                            else
                            {
                                errors.Add(string.Format(CreatePropertiesFromRange.Messages.DuplicateCheckFailedTemplate, 
                                    existCount + CreatePropertiesFromRange.Numeric.IndexOffset));
                                break;
                            }
                        }
                        break;
                    }

                    // Create property with all business logic in service layer
                    var result = await CreateSinglePropertyAsync(request.Template, propertyNo, sequenceNo, ct);

                    if (result != null && result.Success)
                    {
                        _logger.LogInformation("Successfully created property {PropertyNo} at index {Index}", propertyNo, i + CreatePropertiesFromRange.Numeric.IndexOffset);
                        results.Add(result);
                    }
                    else
                    {
                        var errorMessage = result?.Message ?? CreatePropertiesFromRange.Messages.UnknownErrorOccurred;
                        _logger.LogWarning("Failed to create property {PropertyNo} at index {Index}: {Message}",
                            propertyNo, i + CreatePropertiesFromRange.Numeric.IndexOffset, errorMessage);
                        errors.Add(string.Format(CreatePropertiesFromRange.Messages.PropertyCreationFailedTemplate,
                            i + CreatePropertiesFromRange.Numeric.IndexOffset, propertyNo, errorMessage));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = ex switch
                    {
                        DbUpdateException dbEx => string.Format(CreatePropertiesFromRange.Messages.DatabaseErrorTemplate, dbEx.InnerException?.Message ?? dbEx.Message),
                        OperationCanceledException => CreatePropertiesFromRange.Messages.OperationCancelled,
                        ArgumentException argEx => string.Format(CreatePropertiesFromRange.Messages.InvalidArgumentTemplate, argEx.Message),
                        _ => $"{ex.GetType().Name}: {ex.Message}"
                    };
                    
                    _logger.LogError(ex, "Error creating property {PropertyNo} at index {Index}: {ErrorMessage}",
                        rangeValues[i], i + CreatePropertiesFromRange.Numeric.IndexOffset, errorMessage);
                    
                    errors.Add(string.Format(CreatePropertiesFromRange.Messages.PropertyCreationFailedTemplate,
                        i + CreatePropertiesFromRange.Numeric.IndexOffset, rangeValues[i], errorMessage));
                    break;
                }
            }

            if (errors.Count > CreatePropertiesFromRange.Numeric.InitialSuccessCount)
            {
                _logger.LogWarning("Rolling back transaction due to {ErrorCount} errors in property range creation", errors.Count);
                await _unitOfWork.RollbackTransactionAsync(ct);
                return new RangeResult<CreateNewPropertyResponseDto>(
                    SuccessCount: CreatePropertiesFromRange.Numeric.InitialSuccessCount,
                    FailedCount: rangeValues.Count,
                    Results: [],
                    Errors: errors
                );
            }

            _logger.LogInformation("Committing transaction: Successfully created {Count} properties", results.Count);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Property range creation completed successfully: {SuccessCount} properties created", results.Count);
            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: results.Count,
                FailedCount: CreatePropertiesFromRange.Numeric.InitialFailedCount,
                Results: results,
                Errors: null
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Property range creation was cancelled.");
            
            try
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction after cancellation: {Message}", rollbackEx.Message);
            }

            errors.Add(CreatePropertiesFromRange.Messages.OperationCancelled);

            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: CreatePropertiesFromRange.Numeric.InitialSuccessCount,
                FailedCount: rangeValues.Count,
                Results: [],
                Errors: errors
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during property range creation: {Message}", ex.Message);
            
            try
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogInformation("Transaction rolled back successfully after error");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction: {Message}", rollbackEx.Message);
                errors.Add(string.Format(CreatePropertiesFromRange.Messages.RollbackErrorTemplate, rollbackEx.Message));
            }

            errors.Add(string.Format(CreatePropertiesFromRange.Messages.UnexpectedTransactionErrorTemplate, 
                ex.GetType().Name, ex.Message));

            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: CreatePropertiesFromRange.Numeric.InitialSuccessCount,
                FailedCount: rangeValues.Count,
                Results: [],
                Errors: errors
            );
        }
    }

    /// <summary>
    /// Creates a single property with all related entities.
    /// Business logic extracted from repository layer per CLAUDE.md guidelines.
    /// All business rules (category checks, UPIC generation, conditional entity creation) live here in Application layer.
    /// </summary>
    /// <param name="dto">The property creation data.</param>
    /// <param name="propertyNo">The property number for this specific property.</param>
    /// <param name="sequenceNo">The sequence number for this property.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success response with PropertyId when operation succeeds, null on failure.</returns>
    private async Task<CreateNewPropertyResponseDto?> CreateSinglePropertyAsync(CreateNewPropertyDto dto, string propertyNo, int sequenceNo, CancellationToken ct)
    {
        _logger.LogInformation("Creating single property: PropertyNo={PropertyNo}, WardId={WardId}, CategoryId={CategoryId}",propertyNo, dto.WardId, dto.CategoryId);
        
        //STEP 1: Get Ward for UPIC generation (Business Rule)
        var ward = await _wardRepository.GetByIdAsync(dto.WardId, ct);
        _logger.LogInformation("Retrieved ward {WardNo} for property {PropertyNo}", ward?.WardNo, propertyNo);

        // STEP 2: Get Category for business logic (Apartment vs Plot handling) 
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, ct);
        _logger.LogInformation("Retrieved category {CategoryName} for property {PropertyNo}", category?.PropertyCategoryName, propertyNo);

        // STEP 3: Create PropertyEntity using AutoMapper
        var property = _mapper.Map<PropertyEntity>(dto) ?? throw new InvalidOperationException("AutoMapper returned null for CreateNewPropertyDto -> PropertyEntity mapping.");

        // Set range-specific values
        property.PropertyNo = propertyNo?.Trim();
        property.PropertySeqNo = sequenceNo;
        property.PartitionNo = string.Empty;
        property.UPICId = ward != null ? $"{propertyNo}{dto.WardId}{property.PartitionNo}{ward.WardNo}" : $"{propertyNo}{dto.WardId}{property.PartitionNo}";

        await _repository.AddAsync(property, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // STEP 4: Society Creation (Business Rule: Apartment category only) 
        if (category != null && category.PropertyCategoryName.Contains(CreatePropertiesFromRange.CategoryNames.Apartment, CreatePropertiesFromRange.ComparisonOptions.CategoryNameComparison))
        {
            var society = _mapper.Map<SocietyDetailsEntity>(dto);

            society.WingName = "Main Society";
            society.PropertyId = property.Id;

            await _societyRepository.AddAsync(society, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("SocietyDetailsEntity created with Id={SocietyId} for property {PropertyNo}", society.Id, propertyNo);

            // Link society to property
            property.SocietyDetailId = society.Id;
            await _repository.UpdateAsync(property, ct);
            await _unitOfWork.SaveChangesAsync(ct);


            if (dto.LengthMtr != null && dto.WidthMtr != null && dto.TotalAreaSqMtr!=null) 
            {
                // Check if property details already exist
                var existingDetails = await _propertyDetailsRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == property.Id, ct);

                if (existingDetails == null)
                {
                    var propertyDetails = _mapper.Map<PropertyDetailsEntity>(dto);
                    propertyDetails.PropertyId = property.Id;

                    await _propertyDetailsRepository.AddAsync(propertyDetails, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                    _logger.LogInformation("PropertyDetailsEntity created with Id={PropertyDetailsId} for property {PropertyNo}", propertyDetails.Id, propertyNo);

                    // Create RoomWiseSubmissionDetails using AutoMapper
                    var roomWiseDetails = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
                    roomWiseDetails.PropertyId = property.Id;
                    roomWiseDetails.PropertyDetailsId = propertyDetails.Id;

                    await _roomWiseRepository.AddAsync(roomWiseDetails, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                    _logger.LogInformation("RoomWiseSubmissionDetailsEntity created with Id={RoomWiseId} for property {PropertyNo}", roomWiseDetails.Id, propertyNo);
                }
            
            }

        }

        // STEP 5: Assessment Record using AutoMapper
        var propertyAssessment = _mapper.Map<PropertyAssessmentEntity>(dto);
        propertyAssessment.PropertyId = property.Id;

        await _assessmentRepository.AddAsync(propertyAssessment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("PropertyAssessmentEntity created with Id={AssessmentId} for property {PropertyNo}",
            propertyAssessment.Id, propertyNo);

        // STEP 6: PropertyDetails (Business Rule: PLOT category only) 
        if (category != null && category.PropertyCategoryName.Equals(CreatePropertiesFromRange.CategoryNames.Plot, CreatePropertiesFromRange.ComparisonOptions.CategoryNameComparison))
        {
            // Check if property details already exist
            var existingDetails = await _propertyDetailsRepository.GetQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == property.Id, ct);

            if (existingDetails == null)
            {
                var propertyDetails = _mapper.Map<PropertyDetailsEntity>(dto);
                propertyDetails.PropertyId = property.Id;

                await _propertyDetailsRepository.AddAsync(propertyDetails, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("PropertyDetailsEntity created with Id={PropertyDetailsId} for property {PropertyNo}", propertyDetails.Id, propertyNo);

                // Create RoomWiseSubmissionDetails using AutoMapper
                var roomWiseDetails = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
                roomWiseDetails.PropertyId = property.Id;
                roomWiseDetails.PropertyDetailsId = propertyDetails.Id;

                await _roomWiseRepository.AddAsync(roomWiseDetails, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("RoomWiseSubmissionDetailsEntity created with Id={RoomWiseId} for property {PropertyNo}", roomWiseDetails.Id, propertyNo);
            }
        }

        _logger.LogInformation("Successfully completed all entity creation for property {PropertyNo} with PropertyId={PropertyId}", propertyNo, property.Id);
        
        return new CreateNewPropertyResponseDto
        {
            PropertyId = property.Id,
            UPICID = property.UPICId,
            WardID = property.WardId,
            Success = true,
            Message = CreatePropertiesFromRange.Messages.PropertyCreatedSuccessfully
        };
    }
}

