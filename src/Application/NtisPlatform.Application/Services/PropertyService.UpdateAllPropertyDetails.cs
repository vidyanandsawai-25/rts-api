using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Partial class for UpdatePropertyAsync — updates all property details
/// (PropertyMast, SocietyDetailsMast, PropertyMastDetails, PropertyDetails,
/// RoomWiseSubmissionDetails) within a single transaction.
///
/// Business logic lives here in Application layer per CLAUDE.md:
/// "Business logic lives in Application, never Infrastructure." (PR #1)
/// </summary>
public partial class PropertyService
{
    /// <summary>
    /// Updates all property details including basic info, ownership, society,
    /// assessment, property details, and room-wise submission details.
    /// All changes are wrapped in a single transaction for consistency.
    /// </summary>
    /// <param name="propertyId">The property ID to update.</param>
    /// <param name="dto">The update data containing all property fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response indicating success/failure with the updated PropertyId and UPICID.</returns>
    public async Task<UpdateAllPropertyDetailsResponseDto> UpdatePropertyAsync(int propertyId, UpdateAllPropertyDetailsDto dto, CancellationToken ct)
    {
        _logger.LogInformation("Starting UpdatePropertyAsync for PropertyId={PropertyId}", propertyId);

        // ============ STEP 1: Find existing property ============
        var property = await _repository.GetByIdAsync(propertyId, ct);
        if (property == null)
        {
            _logger.LogWarning("Property not found: PropertyId={PropertyId}", propertyId);
            return new UpdateAllPropertyDetailsResponseDto
            {
                PropertyId = propertyId,
                Success = false,
                Message = PropertyConstants.ErrorMessages.NotFound
            };
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            // ============ STEP 2: Update PropertyEntity fields ============
            _logger.LogInformation("Updating PropertyEntity fields for PropertyId={PropertyId}", propertyId);

            var originalUPICId = property.UPICId;
            var originalWardId = property.WardId;

            property = _mapper.Map(dto, property);
            
            // Restore original values that shouldn't be wiped by mapper or correctly assign them
            property.WardId = dto.WardId > 0 ? dto.WardId : originalWardId;
            property.UPICId = originalUPICId;
            
            property.UpdatedBy = dto.UpdatedBy;
            property.UpdatedDate = DateTime.Now;

            await _repository.UpdateAsync(property, ct);

            // ============ STEP 3: Update/Create SocietyDetailsEntity (Apartment category) ============
            var category = await _categoryRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.CategoryId, ct);

            _logger.LogInformation("Retrieved category {CategoryName} for PropertyId={PropertyId}",
                category?.PropertyCategoryName, propertyId);

            SocietyDetailsEntity? newSociety = null;

            if (category != null && category.PropertyCategoryName.Contains(PropertyConstants.Categories.Apartment, StringComparison.OrdinalIgnoreCase))
            {
                if (property.SocietyDetailId.HasValue && property.SocietyDetailId.Value > 0)
                {
                    // Update existing society
                    var society = await _societyRepository.GetByIdAsync(property.SocietyDetailId.Value, ct);
                    if (society != null)
                    {
                        _logger.LogInformation("Updating existing SocietyDetailsEntity Id={SocietyId} for PropertyId={PropertyId}",
                            society.Id, propertyId);

                        society = _mapper.Map(dto, society);
                        society.UpdatedBy = dto.UpdatedBy;
                        society.UpdatedDate = DateTime.UtcNow;

                        await _societyRepository.UpdateAsync(society, ct);
                    }
                }
                else
                {
                    // Create new society and link to property
                    _logger.LogInformation("Creating new SocietyDetailsEntity for PropertyId={PropertyId}", propertyId);

                    newSociety = _mapper.Map<SocietyDetailsEntity>(dto);
                    newSociety.PropertyId = property.Id;
                    newSociety.IsActive = true;
                    newSociety.MarkedForDeletion = false;
                    newSociety.CreatedBy = dto.UpdatedBy;
                    newSociety.CreatedDate = DateTime.UtcNow;

                    await _societyRepository.AddAsync(newSociety, ct);
                    // link (property.SocietyDetailId = newSociety.Id) will be set after the first SaveChangesAsync
                }
            }

            // ============ STEP 4: Update/Create PropertyAssessmentEntity (PropertyMastDetails) ============
            var assessment = await _assessmentRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.PropertyId == propertyId, ct);

            if (assessment != null)
            {
                _logger.LogInformation("Updating PropertyAssessmentEntity for PropertyId={PropertyId}", propertyId);

                assessment = _mapper.Map(dto, assessment);
                assessment.UpdatedBy = dto.UpdatedBy;
                assessment.UpdatedDate = DateTime.UtcNow;

                await _assessmentRepository.UpdateAsync(assessment, ct);
            }
            else
            {
                _logger.LogInformation("Creating PropertyAssessmentEntity for PropertyId={PropertyId}", propertyId);

                var newAssessment = _mapper.Map<PropertyAssessmentEntity>(dto);
                newAssessment.PropertyId = property.Id;
                newAssessment.IsActive = true;
                newAssessment.MarkedForDeletion = false;
                newAssessment.CreatedBy = dto.UpdatedBy;
                newAssessment.CreatedDate = DateTime.UtcNow;

                await _assessmentRepository.AddAsync(newAssessment, ct);
            }

            // ============ STEP 5: Update PropertyDetails + RoomWiseSubmissionDetails ============

            var existingDetails = await _propertyDetailsRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.PropertyId == propertyId, ct);

            if (existingDetails != null)
            {
                _logger.LogInformation("Updating PropertyDetailsEntity Id={PropertyDetailsId} for PropertyId={PropertyId}",
                    existingDetails.Id, propertyId);

                existingDetails = _mapper.Map(dto, existingDetails);
                existingDetails.UpdatedBy = dto.UpdatedBy;
                existingDetails.UpdatedDate = DateTime.UtcNow;

                await _propertyDetailsRepository.UpdateAsync(existingDetails, ct);

                // Update existing RoomWiseSubmissionDetails
                var existingRoomWise = await _roomWiseRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.PropertyDetailsId == existingDetails.Id, ct);

                if (existingRoomWise != null)
                {
                    _logger.LogInformation("Updating RoomWiseSubmissionDetailsEntity Id={RoomWiseId} for PropertyId={PropertyId}",
                        existingRoomWise.Id, propertyId);

                    existingRoomWise = _mapper.Map(dto, existingRoomWise);
                    existingRoomWise.UpdatedBy = dto.UpdatedBy;
                    existingRoomWise.UpdatedDate = DateTime.UtcNow;

                    await _roomWiseRepository.UpdateAsync(existingRoomWise, ct);
                }
                else
                {
                    _logger.LogInformation("Creating RoomWiseSubmissionDetailsEntity for PropertyId={PropertyId}", propertyId);

                    var newRoomWise = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
                    newRoomWise.PropertyId = property.Id;
                    newRoomWise.PropertyDetailsId = existingDetails.Id;
                    newRoomWise.OuterYesNo = false;
                    newRoomWise.MinusYesNo = false;
                    newRoomWise.IsActive = dto.IsActive;
                    newRoomWise.MarkedForDeletion = false;
                    newRoomWise.CreatedBy = dto.UpdatedBy;
                    newRoomWise.CreatedDate = DateTime.UtcNow;

                    await _roomWiseRepository.AddAsync(newRoomWise, ct);
                }
            }
            else
            {
                _logger.LogInformation("Creating PropertyDetailsEntity for PropertyId={PropertyId}", propertyId);

                var newPropertyDetails = _mapper.Map<PropertyDetailsEntity>(dto);
                newPropertyDetails.PropertyId = property.Id;
                newPropertyDetails.IsActive = dto.IsActive;
                newPropertyDetails.MarkedForDeletion = false;
                newPropertyDetails.CreatedBy = dto.UpdatedBy;
                newPropertyDetails.CreatedDate = DateTime.UtcNow;

                await _propertyDetailsRepository.AddAsync(newPropertyDetails, ct);

                // Create RoomWiseSubmissionDetails and link via navigation property 
                var newRoomWise = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
                newRoomWise.PropertyId = property.Id;
                newRoomWise.PropertyDetails = newPropertyDetails; // Use navigation property
                newRoomWise.OuterYesNo = false;
                newRoomWise.MinusYesNo = false;
                newRoomWise.IsActive = dto.IsActive;
                newRoomWise.MarkedForDeletion = false;
                newRoomWise.CreatedBy = dto.UpdatedBy;
                newRoomWise.CreatedDate = DateTime.UtcNow;

                await _roomWiseRepository.AddAsync(newRoomWise, ct);
            }

            // ============ STEP 6: Execute primary SaveChanges ============
            await _unitOfWork.SaveChangesAsync(ct);

            // ============ STEP 7: Link missing IDs that EF Core couldn't map automatically ============
            if (newSociety != null)
            {
                property.SocietyDetailId = newSociety.Id;
                await _repository.UpdateAsync(property, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                _logger.LogInformation("Linked new SocietyDetailsEntity Id={SocietyId} to PropertyId={PropertyId}",
                    newSociety.Id, propertyId);
            }

            // ============ STEP 8: Commit transaction ============
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Successfully completed UpdatePropertyAsync for PropertyId={PropertyId}, UPICId={UPICId}",
                propertyId, property.UPICId);

            return new UpdateAllPropertyDetailsResponseDto
            {
                PropertyId = property.Id,
                UPICID = property.UPICId,
                Success = true,
                Message = PropertyConstants.SuccessMessages.PropertyUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during UpdatePropertyAsync for PropertyId={PropertyId}: {Message}",
                propertyId, ex.Message);

            try
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _unitOfWork.DiscardChanges();
                _logger.LogInformation("Transaction rolled back successfully for PropertyId={PropertyId}", propertyId);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction for PropertyId={PropertyId}: {Message}",
                    propertyId, rollbackEx.Message);
            }

            return new UpdateAllPropertyDetailsResponseDto
            {
                PropertyId = propertyId,
                UPICID= property?.UPICId,
                Success = false,
                Message = PropertyConstants.ErrorMessages.UpdateFailed
            };
        }
        }
}
