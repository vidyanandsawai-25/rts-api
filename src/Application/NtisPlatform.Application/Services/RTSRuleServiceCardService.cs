using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSRuleServiceCardService : IRTSRuleServiceCardService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSServiceEntity, int> _services;
    private readonly IRepository<RTSDepartmentEntity, int> _departments;

    public RTSRuleServiceCardService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSServiceEntity, int> services,
        IRepository<RTSDepartmentEntity, int> departments)
    {
        _rules = rules;
        _services = services;
        _departments = departments;
    }

    public async Task<PagedResult<RTSRuleServiceCardDto>> GetCardsAsync(RTSRuleServiceCardQueryParameters query, CancellationToken ct = default)
    {
        var selectedGroup = query.RuleGroup.Trim().ToUpperInvariant();
        if (selectedGroup.Length == 0) throw new ArgumentException("RuleGroup is required.", nameof(query));

        // RuleMaster owns each action type. Include historical action names in the same tab.
        var actionTypes = selectedGroup switch
        {
            "FEE" => new[] { "BASE_FEE", "ADDON_FEE" },
            "USER_ALLOCATION" => new[] { "STAGE_ALLOCATION", "ASSIGN_TO" },
            _ => Array.Empty<string>()
        };
        if (actionTypes.Length == 0)
            return new PagedResult<RTSRuleServiceCardDto>([], 0, 1, query.PageSize == -1 ? 1 : query.PageSize);

        var cards =
            from service in _services.GetQueryable().AsNoTracking()
            join department in _departments.GetQueryable().AsNoTracking()
                on service.DepartmentId equals department.Id
            join rule in _rules.GetQueryable().AsNoTracking()
                on service.Id equals rule.ServiceId
            where actionTypes.Contains(rule.RuleActionType)
                && service.IsActive
                && (!query.DepartmentId.HasValue || service.DepartmentId == query.DepartmentId.Value)
                && (!query.ServiceId.HasValue || service.Id == query.ServiceId.Value)
            group rule by new { service.Id, service.ServiceName, service.DepartmentId, department.DepartmentName }
            into grouped
            select new RTSRuleServiceCardDto
            {
                ServiceId = grouped.Key.Id,
                ServiceName = grouped.Key.ServiceName,
                DepartmentId = grouped.Key.DepartmentId,
                DepartmentName = grouped.Key.DepartmentName,
                RuleGroup = selectedGroup,
                RuleCount = grouped.Count(),
                ActiveRuleCount = grouped.Count(rule => rule.IsActive)
            };

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            cards = cards.Where(card => card.ServiceName.Contains(search) || card.DepartmentName.Contains(search));
        }

        var total = await cards.CountAsync(ct);
        var ordered = cards.OrderBy(card => card.ServiceName).ThenBy(card => card.ServiceId);
        var items = query.PageSize == -1
            ? await ordered.ToListAsync(ct)
            : await ordered.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var size = query.PageSize == -1 ? Math.Max(1, total) : query.PageSize;
        return new PagedResult<RTSRuleServiceCardDto>(items, total, query.PageNumber, size);
    }
}
