using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ConstructionTypeService : BaseCommonCrudService<ConstructionTypeEntity, ConstructionTypeDto, CreateConstructionTypeDto, UpdateConstructionTypeDto, ConstructionTypeQueryParameters, string>, IConstructionTypeService
{
    public ConstructionTypeService(
        IRepository<ConstructionTypeEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    public async Task<PagedResult<ConstructionTypeDto>> GetAllWithHierarchyAsync(
        ConstructionTypeQueryParameters queryParameters,
        string hierarchyFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply base filters
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);

        // Apply custom hierarchical filter
        query = query.Where(x => x.GroupID.ToLower().Contains(hierarchyFilter.ToLower()));

        // Apply sorting
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and project to DTO
        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<ConstructionTypeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<ConstructionTypeDto>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }
}
