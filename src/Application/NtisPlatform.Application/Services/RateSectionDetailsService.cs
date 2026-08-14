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
    private readonly IRepository<WardEntity> _wardRepository;

    public RateSectionDetailsService(
        IRepository<RateSectionDetailsEntity, int> repository,
        IRepository<WardEntity> wardRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _wardRepository = wardRepository;
    }

    public override async Task<PagedResult<RateSectionDetailsDto>> GetAllAsync(
     RateSectionDetailsQueryParameters queryParameters,
     CancellationToken cancellationToken = default)
    {
        // Filter out orphaned records using an EXISTS clause.
        // This ensures both CountAsync() and Skip/Take correctly agree on the count
        // and only return records that actually have a matching Ward.
        var query = _repository.GetQueryable()
            .AsNoTracking()
            .Where(r => _wardRepository.GetQueryable().Any(w => w.Id == r.WardId));

        // Apply filters
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch pure entities with absolutely no navigation properties or Select blocks
        // This guarantees EF Core will not emit an INNER JOIN that filters out records
        var entities = await query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<RateSectionDetailsDto>>(entities);

        // Manually fetch and populate WardNo in memory to bypass EF Core INNER JOIN optimization bug
        if (items.Any())
        {
            var wardIds = items.Select(x => x.WardId).Distinct().ToList();
            var wards = await _wardRepository.GetQueryable()
                .AsNoTracking()
                .Where(w => wardIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WardNo, cancellationToken);

            foreach (var item in items)
            {
                if (wards.TryGetValue(item.WardId, out var wardNo))
                {
                    item.WardNo = wardNo;
                }
            }
        }

        return new PagedResult<RateSectionDetailsDto>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public override async Task<RateSectionDetailsDto?> GetByIdAsync(
     int id,
     CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null) return null;

        var dto = _mapper.Map<RateSectionDetailsDto>(entity);
        
        var ward = await _wardRepository.GetByIdAsync(entity.WardId, cancellationToken);
        dto.WardNo = ward?.WardNo;

        return dto;
    }
}