// Application/Services/RoomWiseSubmissionDetailsService.cs
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RoomWiseSubmissionDetailsService :  BaseCommonCrudService<RoomWiseSubmissionDetailsEntity, RoomWiseSubmissionDetailsDto, CreateRoomWiseSubmissionDetailsDto, UpdateRoomWiseSubmissionDetailsDto, RoomWiseSubmissionQueryParameters, int>, IRoomWiseSubmissionDetailsService
{
    private readonly IRepository<RoomWiseMinusDataEntity, int> _roomWiseMinusRepository;

    public RoomWiseSubmissionDetailsService( IRepository<RoomWiseSubmissionDetailsEntity, int> repository, IUnitOfWork unitOfWork,  IMapper mapper, IRepository<RoomWiseMinusDataEntity, int> roomWiseMinusRepository) : base(repository, unitOfWork, mapper)
    {
        _roomWiseMinusRepository = roomWiseMinusRepository;
    } 

   public override async Task<PagedResult<RoomWiseSubmissionDetailsDto>> GetAllAsync(
         RoomWiseSubmissionQueryParameters queryParameters,
         CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable()
            .Where(x => !x.MarkedForDeletion && x.PropertyDetailsId == queryParameters.PropertyDetailsId)
            .AsQueryable();

        // Apply filters
        query = query.ApplyFilters(queryParameters);

        // Apply search
        query = query.ApplySearch(queryParameters);

        // Apply sorting
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ProjectTo<RoomWiseSubmissionDetailsDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        // Normalize pagination metadata for unpaged results (PageSize = -1)
        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<RoomWiseSubmissionDetailsDto>(items, totalCount, pageNumber, pageSize);
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
        foreach (var dto in dtos)
        {
            if (dto.Id > 0)
            {
                // UPDATE EXISTING PARENT RECORD
                var existing = await _repository.GetQueryable()
                    .Include(x => x.PropertyRoomMinus)
                    .Where(x => x.Id == dto.Id && x.PropertyDetailsId == propertyDetailsId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existing == null)
                {
                    continue;
                }

                // Update parent fields
                _mapper.Map(dto, existing);
                existing.UpdatedDate = DateTime.Now;
                // Always preserve server-side FK
                existing.PropertyDetailsId = propertyDetailsId;

                /*
                    Existing parent case:

                    RoomWiseMinusData == null
                        => update parent only, do not touch child table

                    RoomWiseMinusData == empty list
                        => update parent only, do not touch child table

                    Child Id > 0
                        => update existing child

                    Child Id == 0
                        => insert new child under existing parent
                */
                if (dto.RoomWiseMinusData?.Any() == true)
                {
                    existing.PropertyRoomMinus ??= new List<RoomWiseMinusDataEntity>();

                    foreach (var minusDto in dto.RoomWiseMinusData)
                    {
                        if (minusDto.Id > 0)
                        {
                            // UPDATE EXISTING CHILD RECORD
                            var existingChild = existing.PropertyRoomMinus
                                .FirstOrDefault(m => m.Id == minusDto.Id);

                            if (existingChild != null)
                            {
                                _mapper.Map(minusDto, existingChild);

                                // FK should never change on update
                                existingChild.RoomWiseSubmissionId = existing.Id;

                                existingChild.UpdatedDate = DateTime.Now;
                            }
                        }
                        else
                        {
                            // INSERT NEW CHILD RECORD UNDER EXISTING PARENT
                            var minus = _mapper.Map<RoomWiseMinusDataEntity>(minusDto);

                            minus.Id = 0;
                            minus.RoomWiseSubmissionId = existing.Id;
                            minus.CreatedDate = DateTime.Now;
                            minus.IsActive = true;

                            existing.PropertyRoomMinus.Add(minus);
                        }
                    }
                }

                await _repository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                // INSERT NEW PARENT RECORD
                var entity = _mapper.Map<RoomWiseSubmissionDetailsEntity>(dto);

                entity.Id = 0;

                // Use method parameter, not DTO PropertyDetailsId
                entity.PropertyDetailsId = propertyDetailsId;

                /*
                    New parent case:

                    RoomWiseMinusData == null
                        => insert parent only

                    RoomWiseMinusData == empty list
                        => insert parent only

                    RoomWiseMinusData has records
                        => insert parent and insert child records
                */
                if (dto.RoomWiseMinusData?.Any() == true)
                {
                    entity.PropertyRoomMinus = dto.RoomWiseMinusData
                        .Select(minusDto =>
                        {
                            var minus = _mapper.Map<RoomWiseMinusDataEntity>(minusDto);
                             minus.CreatedDate = DateTime.Now;
                            minus.IsActive = true;

                            return minus;
                        })
                        .ToList();
                }

                await _repository.AddAsync(entity, cancellationToken);
            }
        }
    }

    public async Task DeleteByPropertyIdAsync( int propertyDetailsId, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetQueryable()
            .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var entity in existing)
        {
            // Soft-delete child RoomWiseMinusData records first
            var minusRecords = await _roomWiseMinusRepository.GetQueryable()
                .Where(m => m.RoomWiseSubmissionId == entity.Id && m.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var minus in minusRecords)
                await _roomWiseMinusRepository.DeleteAsync(minus.Id, cancellationToken);

            // Then soft-delete the parent submission record
            await _repository.DeleteAsync(entity.Id, cancellationToken);
        }
    }

   
}