using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Rules
{
    public class PropertyRuleApplicationLogService : IPropertyRuleApplicationLogService
    {
        private readonly IRepository<PropertyRuleApplicationLogEntity, int> _repository;
        private readonly IMapper _mapper;

        public PropertyRuleApplicationLogService(
            IRepository<PropertyRuleApplicationLogEntity, int> repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<PropertyRuleApplicationLogDto>> GetLogsAsync(
            PropertyRuleApplicationLogQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            // ✅ Only show data where IsActive = 1 and MarkedForDeletion = 0
            var query = _repository.GetQueryable()
                .Where(x => x.IsActive && !x.MarkedForDeletion);

            // Apply standard filters
            query = query.ApplyFilters(queryParameters);

            // Custom search logic: Search by RuleName (via SearchTerm string) OR if SearchTerm is numeric, match PropertyId / PropertyDetailsId
            if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
            {
                var searchTerm = queryParameters.SearchTerm.Trim();
                if (int.TryParse(searchTerm, out int parsedId))
                {
                    query = query.Where(x => x.PropertyId == parsedId || x.PropertyDetailsId == parsedId || x.RuleName.Contains(searchTerm));
                }
                else
                {
                    query = query.ApplySearch(queryParameters);
                }
            }

            // Apply sorting
            query = query.ApplySort(queryParameters);

            var totalCount = await query.CountAsync(cancellationToken);

            var pagedQuery = query
                .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

            var items = await pagedQuery
                .ProjectTo<PropertyRuleApplicationLogDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
            var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

            return new PagedResult<PropertyRuleApplicationLogDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PropertyRuleApplicationLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);

            if (entity == null)
                return null;

            return _mapper.Map<PropertyRuleApplicationLogDto>(entity);
        }
    }
}
