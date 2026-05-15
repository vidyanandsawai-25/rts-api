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

public class RateSectionDetailsService : BaseCommonCrudService<RateSectionDetailsEntity, RateSectionDetailsDto, CreateRateSectionDetailsDto, UpdateRateSectionDetailsDto, RateSectionDetailsQueryParameters, int>, IRateSectionDetailsService
{
    public RateSectionDetailsService(
        IRepository<RateSectionDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    public override async Task<PagedResult<RateSectionDetailsDto>> GetAllAsync(
     RateSectionDetailsQueryParameters queryParameters,
     CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable()
            .Include(x => x.Ward)
            .AsQueryable();  // Convert back to IQueryable<T>

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
            .ProjectTo<RateSectionDetailsDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);


        return new PagedResult<RateSectionDetailsDto>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public override async Task<RateSectionDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.Ward)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : _mapper.Map<RateSectionDetailsDto>(entity);
    }
}

