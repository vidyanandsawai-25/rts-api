using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Utilities;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Partial class for PropertyService containing mapped old property detail overrides.
/// </summary>
public partial class PropertyService
{
    public async Task<PagedResult<MappedOldPropertyMastDto>?> GetMappedOldPropertyDetailsAsync(MappedOldPropertyQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        // Fetch PropertyMapDetails for this new property using the generic method
        var mapDetailsQuery = _propertyMapDetailRepository.GetQueryable()
            .Where(pmd => pmd.PropertyIdNew == queryParameters.PropertyId && pmd.IsActive);

        int totalCount = await mapDetailsQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<MappedOldPropertyMastDto>();
        }

        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(queryParameters.PageNumber, queryParameters.PageSize, totalCount);

        var mapDetails = await mapDetailsQuery
            .OrderBy(pmd => pmd.Id) 
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var oldPropertyIds = mapDetails
            .Where(pmd => pmd.PropertyIdOld.HasValue)
            .Select(pmd => pmd.PropertyIdOld!.Value)
            .Distinct()
            .ToList();

        // Fetch old master properties
        var oldProperties = await _propertyOldRepository.GetQueryable()
            .Where(pmo => oldPropertyIds.Contains(pmo.Id) && pmo.IsActive && !pmo.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Fetch old property floor details
        var oldDetailsRepo = _propertyDetailsOldRepository ?? _serviceProvider?.GetService<IRepository<PropertyDetailsOldEntity, int>>();

        var oldPropertyDetails = oldDetailsRepo != null
            ? await oldDetailsRepo.GetQueryable()
                .Where(pdo => oldPropertyIds.Contains(pdo.PropertyMastOldId) && pdo.IsActive && !pdo.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyDetailsOldEntity>();

        var oldPropertyLookup = oldProperties.ToDictionary(p => p.Id);
        var oldDetailsLookup = oldPropertyDetails
            .GroupBy(d => d.PropertyMastOldId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Combine data into DTOs
        var mappedOldProperties = new List<MappedOldPropertyMastDto>();

        foreach (var pmd in mapDetails)
        {
            if (pmd.PropertyIdOld.HasValue && oldPropertyLookup.TryGetValue(pmd.PropertyIdOld.Value, out var oldProp))
            {
                oldDetailsLookup.TryGetValue(oldProp.Id, out var floorDetails);

                var propertyDetails = _mapper.Map<MappedOldPropertyMastDto>(oldProp);
                propertyDetails.MappedNewBuildingId = pmd.PropertyIdNew; // Keep track of the link

                if (floorDetails != null)
                {
                    propertyDetails.FloorDetails = _mapper.Map<List<MappedOldPropertyFloorDetailDto>>(floorDetails);
                }

                mappedOldProperties.Add(propertyDetails);
            }
        }

        return new PagedResult<MappedOldPropertyMastDto>
        {
            Items = mappedOldProperties,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
