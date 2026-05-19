
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RenterMastService : BaseCommonCrudService<RenterMastEntity, RenterMastDto, CreateRenterMastDto, UpdateRenterMastDto, PropertyDetailsQueryParameters, int>, IRenterMastService
{
 
    public RenterMastService( IRepository<RenterMastEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper):base(repository, unitOfWork, mapper) { }

    public async Task CreateRangeAsync( int propertyDetailsId, IEnumerable<CreateRenterMastDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
        {
            var entity = _mapper.Map<RenterMastEntity>(dto);
            entity.PropertyDetailsId = propertyDetailsId;
            await _repository.AddAsync(entity, cancellationToken);
        }
    }

    public async Task UpdateRangeAsync( int propertyDetailsId, IEnumerable<UpdateRenterMastDto> dtos, CancellationToken cancellationToken = default)
    {
        var dtoList = dtos.ToList();
        if (!dtoList.Any())
            return;

        var dtoIds = dtoList.Select(d => d.Id).ToList();

        // Load all entities in a single query
        var entities = await _repository.GetQueryable()
            .Where(x => x.PropertyDetailsId == propertyDetailsId && dtoIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var entityDict = entities.ToDictionary(e => e.Id);

        foreach (var dto in dtoList)
        {
            if (!entityDict.TryGetValue(dto.Id, out var entity))
            {
                throw new KeyNotFoundException(
                    $"Renter mast with Id {dto.Id} was not found for PropertyDetailsId {propertyDetailsId}.");
            }

            _mapper.Map(dto, entity);
            entity.PropertyDetailsId = propertyDetailsId;

            await _repository.UpdateAsync(entity, cancellationToken);
        }
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