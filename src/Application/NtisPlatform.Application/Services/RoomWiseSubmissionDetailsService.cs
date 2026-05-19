// Application/Services/RoomWiseSubmissionDetailsService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RoomWiseSubmissionDetailsService :  BaseCommonCrudService<RoomWiseSubmissionDetailsEntity, RoomWiseSubmissionDetailsDto, CreateRoomWiseSubmissionDetailsDto, UpdateRoomWiseSubmissionDetailsDto, PropertyDetailsQueryParameters, int>, IRoomWiseSubmissionDetailsService
{
     
    public RoomWiseSubmissionDetailsService( IRepository<RoomWiseSubmissionDetailsEntity, int> repository, IUnitOfWork unitOfWork,  IMapper mapper) : base(repository, unitOfWork, mapper)
    {
    }

    

    public async Task CreateRangeAsync( int propertyDetailsId, IEnumerable<CreateRoomWiseSubmissionDetailsDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
        {
            var entity = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
            entity.PropertyDetailsId = propertyDetailsId;

            // ✅ Set audit fields for children explicitly
            if (dto.RoomWiseMinusData?.Any() == true)
            {
                entity.PropertyRoomMinus = dto.RoomWiseMinusData
                    .Select(m =>
                    {
                        var minus = _mapper.Map<RoomWiseMinusDataEntity>(m);
                        minus.CreatedDate = DateTime.Now;
                        
                        minus.IsActive = true;
                        return minus;
                    })
                    .ToList();
            }

            await _repository.AddAsync(entity, cancellationToken);
        }
    }

    public async Task UpdateRangeAsync( int propertyDetailsId, IEnumerable<UpdateRoomWiseSubmissionDetailsDto> dtos, CancellationToken cancellationToken = default)
    {
        // Process each DTO - update only what's sent, don't delete orphans
        foreach (var dto in dtos)
        {
            if (dto.Id > 0)
            {
                // UPDATE EXISTING RECORD
                var existing = await _repository.GetQueryable()
                    .Include(x => x.PropertyRoomMinus)
                    .Where(x => x.Id == dto.Id && x.PropertyDetailsId == propertyDetailsId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existing != null)
                {
                    // Map scalar fields from DTO to tracked entity
                    _mapper.Map(dto, existing);

                    // Always preserve server-side parent FK
                    existing.PropertyDetailsId = propertyDetailsId;

                    // Handle child collection (PropertyRoomMinus) - SOFT DELETE pattern
                    // Only process children if RoomWiseMinusData is explicitly provided (not null)
                    // - null = no change (skip child processing)
                    // - empty list = delete all children
                    // - list with items = keep/update these, soft-delete others

                    if (dto.RoomWiseMinusData is not null)
                    {
                        var dtoMinusIds = dto.RoomWiseMinusData
                            .Where(m => m.Id > 0)
                            .Select(m => m.Id)
                            .ToHashSet();

                        // Soft-delete removed children (those that exist in DB but not in DTO)
                        if (existing.PropertyRoomMinus?.Any() == true)
                        {
                            foreach (var existingMinus in existing.PropertyRoomMinus.Where(m => m.IsActive))
                            {
                                if (!dtoMinusIds.Contains(existingMinus.Id))
                                {
                                    // Soft-delete: set flags instead of removing from collection
                                    existingMinus.IsActive = false;
                                    existingMinus.MarkedForDeletion = true;
                                    existingMinus.MarkedForDeletionDate = DateTime.Now;
                                }
                            }
                        }

                        // Update or add children
                        if (dto.RoomWiseMinusData.Any())
                        {
                            foreach (var minusDto in dto.RoomWiseMinusData)
                            {
                                if (minusDto.Id > 0)
                                {
                                    // Update existing child
                                    var existingChild = existing.PropertyRoomMinus?
                                        .FirstOrDefault(m => m.Id == minusDto.Id);

                                    if (existingChild != null)
                                    {
                                        _mapper.Map(minusDto, existingChild);
                                        existingChild.UpdatedDate = DateTime.Now;
                                    }
                                }
                                else
                                {
                                    // Add new child
                                    var minus = _mapper.Map<RoomWiseMinusDataEntity>(minusDto);
                                    minus.CreatedDate = DateTime.Now;
                                    minus.IsActive = true;
                                    minus.RoomWiseSubmissionId = existing.Id;

                                    existing.PropertyRoomMinus ??= new List<RoomWiseMinusDataEntity>();
                                    existing.PropertyRoomMinus.Add(minus);
                                }
                            }
                        }
                    }

                    await _repository.UpdateAsync(existing, cancellationToken);
                }
            }
            else
            {
                // ADD NEW RECORD (Id = 0 or not provided)
                var entity = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);
                entity.PropertyDetailsId = propertyDetailsId;

                if (dto.RoomWiseMinusData?.Any() == true)
                {
                    entity.PropertyRoomMinus = dto.RoomWiseMinusData
                        .Select(m =>
                        {
                            var minus = _mapper.Map<RoomWiseMinusDataEntity>(m);
                            minus.CreatedDate = DateTime.Now;
                            minus.IsActive = true;
                            return minus;
                        })
                        .ToList();
                }

                await _repository.AddAsync(entity, cancellationToken);
            }
        }

        // NOTE: We do NOT delete orphaned parent records here.
        // Orphaned records should only be soft-deleted via explicit DeleteAsync call.
        // This ensures update operations only modify what's sent, not delete what's missing.
    }
    public async Task DeleteByPropertyIdAsync( int propertyDetailsId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetQueryable()
            .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var entity in existing)
            await _repository.DeleteAsync(entity.Id, cancellationToken);
    }

   
}