using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Rules;
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
                .Select(log => new PropertyRuleApplicationLogDto
                {
                    Id = log.Id,
                    PropertyId = log.PropertyId,
                    PropertyDetailsId = log.PropertyDetailsId,
                    FinanceYear = log.FinanceYear,
                    RuleCategory = log.RuleCategory,
                    RuleCode = log.RuleCode,
                    RuleName = log.RuleName,
                    EffectType = log.EffectType,
                    EffectValue = log.EffectValue,
                    ApplyRate = log.ApplyRate,
                    BaseValue = log.BaseValue,
                    ComputedValue = log.ComputedValue,
                    CumulativeValue = log.CumulativeValue,
                    ApplyOrder = log.ApplyOrder,
                    StopProcessing = log.StopProcessing,
                    AppliedAt = log.AppliedAt,
                    IsActive = log.IsActive,
                    MarkedForDeletion = log.MarkedForDeletion,
                    CreatedDate = log.CreatedDate,
                    UpdatedDate = log.UpdatedDate,
                    CreatedBy = log.CreatedBy,
                    UpdatedBy = log.UpdatedBy,
                    RuleScopeId = log.RuleScopeId,
                    RuleScopeName = log.RuleScopeName
                })
                .ToListAsync(cancellationToken);

            var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
            var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

            return new PagedResult<PropertyRuleApplicationLogDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PropertyRuleApplicationLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var logQuery = _repository.GetQueryable()
                .Where(x => x.Id == id && x.IsActive && !x.MarkedForDeletion);

            return await logQuery
                .Select(log => new PropertyRuleApplicationLogDto
                {
                    Id = log.Id,
                    PropertyId = log.PropertyId,
                    PropertyDetailsId = log.PropertyDetailsId,
                    FinanceYear = log.FinanceYear,
                    RuleCategory = log.RuleCategory,
                    RuleCode = log.RuleCode,
                    RuleName = log.RuleName,
                    EffectType = log.EffectType,
                    EffectValue = log.EffectValue,
                    ApplyRate = log.ApplyRate,
                    BaseValue = log.BaseValue,
                    ComputedValue = log.ComputedValue,
                    CumulativeValue = log.CumulativeValue,
                    ApplyOrder = log.ApplyOrder,
                    StopProcessing = log.StopProcessing,
                    AppliedAt = log.AppliedAt,
                    IsActive = log.IsActive,
                    MarkedForDeletion = log.MarkedForDeletion,
                    CreatedDate = log.CreatedDate,
                    UpdatedDate = log.UpdatedDate,
                    CreatedBy = log.CreatedBy,
                    UpdatedBy = log.UpdatedBy,
                    RuleScopeId = log.RuleScopeId,
                    RuleScopeName = log.RuleScopeName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task DeleteByPropertyDetailsIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            await _repository.GetQueryable()
                .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive && !x.MarkedForDeletion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsActive,              false)
                    .SetProperty(x => x.MarkedForDeletion,     true)
                    .SetProperty(x => x.MarkedForDeletionDate, now)
                    .SetProperty(x => x.UpdatedDate,           now),
                    cancellationToken);
        }

        public async Task DeleteByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            await _repository.GetQueryable()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsActive,              false)
                    .SetProperty(x => x.MarkedForDeletion,     true)
                    .SetProperty(x => x.MarkedForDeletionDate, now)
                    .SetProperty(x => x.UpdatedDate,           now),
                    cancellationToken);
        }
    }
}
