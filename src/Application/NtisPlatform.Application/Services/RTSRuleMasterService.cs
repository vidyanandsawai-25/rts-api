using System.Globalization;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Persists fee and user-allocation rules in the shared RTS.RuleMaster table.
/// Rule-group-specific services provide validation and runtime evaluation only.
/// </summary>
public class RTSRuleMasterService : BaseCommonCrudService<RTSRuleMasterEntity, RTSRuleMasterDto,
    CreateRTSRuleMasterRequestDto, UpdateRTSRuleMasterRequestDto, RTSRuleMasterQueryParameters, int>,
    IRTSRuleMasterService
{
    private readonly IRtsFeeEvaluationRuleService _feeRules;
    private readonly IRTSStageAllocationRuleService _allocationRules;
    private readonly IRepository<RTSRuleConditionEntity, int> _conditionRepository;
    private readonly IRepository<UserEntity, int> _users;

    public RTSRuleMasterService(
        IRepository<RTSRuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRtsFeeEvaluationRuleService feeRules,
        IRTSStageAllocationRuleService allocationRules,
        IRepository<RTSRuleConditionEntity, int> conditionRepository,
        IRepository<UserEntity, int> users)
        : base(repository, unitOfWork, mapper)
    {
        _feeRules = feeRules;
        _allocationRules = allocationRules;
        _conditionRepository = conditionRepository;
        _users = users;
    }

    protected override IQueryable<RTSRuleMasterEntity> ApplyIncludes(IQueryable<RTSRuleMasterEntity> query) =>
        query.Include(x => x.Conditions);

    public override async Task<PagedResult<RTSRuleMasterDto>> GetAllAsync(
        RTSRuleMasterQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        // Let the base service retain the standard filtering, sorting and paging behavior,
        // then rehydrate the selected rules into their recursive condition shape.
        var page = await base.GetAllAsync(queryParameters, cancellationToken);
        var ids = page.Items.Select(rule => rule.Id).Where(id => id > 0).ToList();
        if (ids.Count == 0)
            return page;

        var rules = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(rule => rule.Conditions)
            .Where(rule => ids.Contains(rule.Id))
            .ToDictionaryAsync(rule => rule.Id, cancellationToken);

        var items = page.Items.Select(summary =>
                rules.TryGetValue(summary.Id, out var rule)
                    ? ToRuleDto(rule)
                    : summary)
            .ToList();

        await PopulateUserDetailsAsync(items, cancellationToken);
        return new PagedResult<RTSRuleMasterDto>(
            items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    public override async Task<RTSRuleMasterDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var dto = ToRuleDto(entity);
        await PopulateUserDetailsAsync([dto], cancellationToken);
        return dto;
    }

    public async Task<RTSRuleMasterResponseDto> CreateRulesAsync(
        CreateRTSRuleMasterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required.");

        var nextPriorityByGroup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in request.Rules.Select(rule => NormalizeGroup(rule.RuleGroup)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var highestPriority = await _repository.GetQueryable()
                .Where(rule => rule.ServiceId == request.ServiceId && rule.RuleGroup == group)
                .Select(rule => (int?)rule.Priority)
                .MaxAsync(cancellationToken) ?? 0;
            nextPriorityByGroup[group] = highestPriority + 1;
        }

        foreach (var rule in request.Rules)
        {
            rule.Priority = nextPriorityByGroup[NormalizeGroup(rule.RuleGroup)]++;
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            if (IsFee(rule.RuleGroup))
            {
                await _feeRules.ValidateRuleAsync(
                    request.ServiceId,
                    rule.RuleActionType,
                    rule.ConditionMatchType,
                    rule.CalculationType,
                    rule.RateAmount,
                    rule.QuantityFieldDefinitionId,
                    rule.Conditions,
                    cancellationToken);
            }
            else
            {
                await _allocationRules.ValidateAsync(
                    request.ServiceId,
                    rule.UserId,
                    rule.ActionValue,
                    rule.ConditionMatchType,
                    rule.Conditions,
                    cancellationToken);
            }
        }

        var entities = request.Rules.Select(rule =>
        {
            var entity = new RTSRuleMasterEntity
            {
                ServiceId = request.ServiceId,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            Apply(rule, entity);
            return entity;
        }).ToList();

        await _repository.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Response(request.ServiceId, entities);
    }

    public async Task<RTSRuleMasterResponseDto> UpdateRulesAsync(
        int serviceId,
        UpdateRTSRuleMasterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0 || request.ServiceId != serviceId)
            throw new InvalidOperationException("Service ID does not match the rule request.");
        if (request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required for update.");

        var ids = request.Rules.Select(x => x.Id ?? 0).ToList();
        if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            throw new InvalidOperationException("Every updated rule needs a distinct valid ID.");

        var entities = await _repository.GetQueryable()
            .Include(x => x.Conditions)
            .Where(x => ids.Contains(x.Id) && x.ServiceId == serviceId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (entities.Count != ids.Count)
            throw new InvalidOperationException("One or more rules were not found for this service.");

        foreach (var rule in request.Rules)
        {
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            var entity = entities[rule.Id!.Value];
            var requestedGroup = NormalizeGroup(rule.RuleGroup);
            if (!string.Equals(entity.RuleGroup, requestedGroup, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A rule cannot be moved to another rule group.");

            if (IsFee(requestedGroup))
            {
                await _feeRules.ValidateRuleAsync(
                    serviceId,
                    rule.RuleActionType,
                    rule.ConditionMatchType,
                    rule.CalculationType,
                    rule.RateAmount,
                    rule.QuantityFieldDefinitionId,
                    rule.Conditions,
                    cancellationToken);
            }
            else
            {
                await _allocationRules.ValidateAsync(
                    serviceId,
                    rule.UserId,
                    rule.ActionValue,
                    rule.ConditionMatchType,
                    rule.Conditions,
                    cancellationToken);
            }

        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // RuleCondition has a unique sibling-order index.  Deleting the old tree in a
            // separate save prevents an old top-level node and its replacement from both
            // occupying (RuleMasterId, ParentConditionId, ConditionOrder) during one flush.
            await DeleteConditionTreesAsync(entities.Values, cancellationToken);

            foreach (var rule in request.Rules)
            {
                var entity = entities[rule.Id!.Value];
                Apply(rule, entity);
                entity.IsActive = rule.IsActive;
                entity.UpdatedBy = request.UpdatedBy;
                entity.UpdatedDate = DateTime.UtcNow;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }

        return Response(serviceId, entities.Values);
    }

    public async Task UpdatePrioritiesAsync(
        UpdateRTSRulePrioritiesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var group = NormalizeGroup(request.RuleGroup);
        if (request.ServiceId <= 0 || group is not ("FEE" or "USER_ALLOCATION"))
            throw new InvalidOperationException("Select a valid service and rule group.");

        var requested = request.Priorities;
        var ids = requested.Select(item => item.RuleId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw new InvalidOperationException("Each rule can appear only once in the priority order.");
        if (requested.Select(item => item.Priority).Distinct().Count() != requested.Count ||
            !requested.Select(item => item.Priority).OrderBy(priority => priority)
                .SequenceEqual(Enumerable.Range(1, requested.Count)))
            throw new InvalidOperationException("Priorities must be consecutive values starting at 1.");

        var rules = await _repository.GetQueryable()
            .Where(rule => rule.ServiceId == request.ServiceId && rule.RuleGroup == group)
            .ToListAsync(cancellationToken);
        if (rules.Count != requested.Count || !rules.Select(rule => rule.Id).OrderBy(id => id)
                .SequenceEqual(ids.OrderBy(id => id)))
            throw new InvalidOperationException("The priority request must include every rule in the selected service and group.");

        var priorityById = requested.ToDictionary(item => item.RuleId, item => item.Priority);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var rule in rules)
            {
                rule.Priority = priorityById[rule.Id];
                rule.UpdatedBy = request.UpdatedBy;
                rule.UpdatedDate = DateTime.UtcNow;
            }
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    public async Task SetActiveAsync(
        int id,
        UpdateRTSRuleMasterStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetQueryable()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException("The rule was not found.");

        rule.IsActive = request.IsActive;
        rule.UpdatedBy = request.UpdatedBy;
        rule.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PermanentlyDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetQueryable()
            .Include(entity => entity.Conditions)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (rule == null)
            throw new InvalidOperationException("The rule was not found.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // The self-reference is Restrict, so remove descendants before their parents.
            // This also makes the operation safe for databases where the RuleMaster cascade
            // was created manually rather than by the EF migration.
            await DeleteConditionTreesAsync([rule], cancellationToken);
            await _repository.HardDeleteAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    public Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(
        RTSFeeEvaluationRequestDto request,
        CancellationToken cancellationToken = default) =>
        _feeRules.EvaluateFeeAsync(request, cancellationToken);

    private static void ValidateAction(string? group, string? action)
    {
        var normalizedGroup = NormalizeGroup(group);
        var normalizedAction = action?.Trim().ToUpperInvariant();
        var supported = normalizedGroup switch
        {
            "FEE" => normalizedAction is "BASE_FEE" or "ADDON_FEE",
            "USER_ALLOCATION" => normalizedAction == "STAGE_ALLOCATION",
            _ => false
        };
        if (!supported)
            throw new InvalidOperationException("Use BASE_FEE or ADDON_FEE for FEE rules, and STAGE_ALLOCATION for USER_ALLOCATION rules.");
    }

    private static void ValidateDetails(string? description, int priority)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200)
            throw new InvalidOperationException("Enter a rule description of up to 200 characters.");
        if (priority < 1)
            throw new InvalidOperationException("Rule priority must be a positive number.");
    }

    private static void Apply(CreateRTSRuleDto source, RTSRuleMasterEntity target)
    {
        target.RuleDescription = source.RuleDescription?.Trim() ?? string.Empty;
        target.RuleGroup = NormalizeGroup(source.RuleGroup);
        target.RuleActionType = source.RuleActionType?.Trim().ToUpperInvariant() ?? string.Empty;
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.Priority = source.Priority;
        ApplyGroupValues(source.RuleGroup, source.CalculationType, source.RateAmount,
            source.QuantityFieldDefinitionId, source.UserId, source.ActionValue, target);
        ReplaceConditions(source.Conditions, target);
    }

    private static void Apply(UpdateRTSRuleDto source, RTSRuleMasterEntity target)
    {
        target.RuleDescription = source.RuleDescription?.Trim() ?? string.Empty;
        target.RuleGroup = NormalizeGroup(source.RuleGroup);
        target.RuleActionType = source.RuleActionType?.Trim().ToUpperInvariant() ?? string.Empty;
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.Priority = source.Priority;
        ApplyGroupValues(source.RuleGroup, source.CalculationType, source.RateAmount,
            source.QuantityFieldDefinitionId, source.UserId, source.ActionValue, target);
        ReplaceConditions(source.Conditions, target);
    }

    private static void ApplyGroupValues(
        string? group,
        string? calculationType,
        decimal? rateAmount,
        int? quantityFieldDefinitionId,
        int? userId,
        string? actionValue,
        RTSRuleMasterEntity target)
    {
        if (IsFee(group))
        {
            target.CalculationType = calculationType?.Trim().ToUpperInvariant();
            target.RateAmount = rateAmount;
            target.QuantityFieldDefinitionId = quantityFieldDefinitionId;
            target.UserId = null;
            target.ActionValue = null;
        }
        else
        {
            target.CalculationType = null;
            target.RateAmount = null;
            target.QuantityFieldDefinitionId = null;
            target.UserId = userId;
            target.ActionValue = actionValue;
        }
        target.IsCumulative = false;
    }

    private static bool IsFee(string? group) =>
        string.Equals(group?.Trim(), "FEE", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeGroup(string? group) =>
        group?.Trim().ToUpperInvariant() ?? string.Empty;

    private async Task DeleteConditionTreesAsync(
        IEnumerable<RTSRuleMasterEntity> rules,
        CancellationToken cancellationToken)
    {
        var conditions = rules.SelectMany(rule => rule.Conditions).ToList();
        if (conditions.Count == 0)
            return;

        var byId = conditions.Where(condition => condition.Id > 0)
            .ToDictionary(condition => condition.Id);
        foreach (var condition in conditions.OrderByDescending(condition =>
                     GetConditionDepth(condition, byId)))
        {
            await _conditionRepository.HardDeleteAsync(condition, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The rows are no longer tracked after the hard delete.  Clear their navigation
        // collections before the new tree is attached to the same rule master.
        foreach (var rule in rules)
            rule.Conditions.Clear();
    }

    private static int GetConditionDepth(
        RTSRuleConditionEntity condition,
        IReadOnlyDictionary<int, RTSRuleConditionEntity> byId)
    {
        var depth = 0;
        var parentId = condition.ParentConditionId;
        var visited = new HashSet<int>();
        while (parentId.HasValue && byId.TryGetValue(parentId.Value, out var parent) &&
               visited.Add(parent.Id))
        {
            depth++;
            parentId = parent.ParentConditionId;
        }

        return depth;
    }

    private static void ReplaceConditions(
        IEnumerable<RTSRuleConditionDto> conditions,
        RTSRuleMasterEntity target)
    {
        target.Conditions.Clear();

        void AddNodes(
            IEnumerable<RTSRuleConditionDto> source,
            RTSRuleConditionEntity? parent)
        {
            var ordered = source.OrderBy(x => x.ConditionOrder).ToList();
            for (var index = 0; index < ordered.Count; index++)
            {
                var condition = ordered[index];
                var type = condition.ConditionType.Trim().ToUpperInvariant();
                var row = new RTSRuleConditionEntity
                {
                    ConditionType = type,
                    ConditionMatchType = type == "GROUP"
                        ? condition.ConditionMatchType?.Trim().ToUpperInvariant()
                        : null,
                    FieldDefinitionId = type == "PREDICATE" ? condition.FieldDefinitionId : null,
                    ComparisonOperator = type == "PREDICATE"
                        ? condition.ComparisonOperator?.Trim().ToUpperInvariant()
                        : null,
                    ExpectedValue = type == "PREDICATE" ? condition.ExpectedValue?.Trim() : null,
                    ConditionOrder = index + 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ParentCondition = parent
                };
                target.Conditions.Add(row);
                if (parent != null)
                    parent.ChildConditions.Add(row);
                AddNodes(condition.Children, row);
            }
        }

        AddNodes(conditions, null);
    }

    private RTSRuleMasterResponseDto Response(
        int serviceId,
        IEnumerable<RTSRuleMasterEntity> entities)
    {
        var rules = entities.Select(x =>
            ToRuleDto(x)).ToList();
        return new RTSRuleMasterResponseDto
        {
            ServiceId = serviceId,
            RuleCount = rules.Count,
            Rules = rules
        };
    }

    private RTSRuleMasterDto ToRuleDto(RTSRuleMasterEntity rule)
    {
        var dto = _mapper.Map<RTSRuleMasterDto>(rule);
        var nodes = rule.Conditions.ToList();
        var children = nodes.ToDictionary(
            node => node,
            parent => nodes.Where(child => IsChildOf(child, parent))
                .OrderBy(child => child.ConditionOrder)
                .ThenBy(child => child.Id)
                .ToList());
        var nestedNodes = children.Values.SelectMany(value => value).ToHashSet();

        dto.Conditions = nodes
            .Where(node => !nestedNodes.Contains(node))
            .OrderBy(node => node.ConditionOrder)
            .ThenBy(node => node.Id)
            .Select(node => ToConditionDto(node, children, new HashSet<RTSRuleConditionEntity>()))
            .ToList();
        return dto;
    }

    private async Task PopulateUserDetailsAsync(
        IReadOnlyCollection<RTSRuleMasterDto> rules,
        CancellationToken cancellationToken)
    {
        var userIds = rules
            .Where(rule => string.Equals(rule.RuleGroup, "USER_ALLOCATION", StringComparison.OrdinalIgnoreCase) &&
                           rule.UserId is > 0)
            .Select(rule => rule.UserId!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count == 0)
            return;

        var users = await _users.GetQueryable()
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id) && !user.MarkedForDeletion)
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.FirstName,
                user.MiddleName,
                user.LastName
            })
            .ToListAsync(cancellationToken);
        var detailsById = users.ToDictionary(user => user.Id, user => new RTSRuleUserDetailsDto
        {
            Id = user.Id,
            UserName = user.UserName,
            UserFullName = string.Join(" ", new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)))
        });

        foreach (var rule in rules)
        {
            if (rule.UserId is > 0 && detailsById.TryGetValue(rule.UserId.Value, out var details))
                rule.UserDetails = details;
        }
    }

    private static bool IsChildOf(
        RTSRuleConditionEntity child,
        RTSRuleConditionEntity parent) =>
        ReferenceEquals(child.ParentCondition, parent) ||
        (child.ParentConditionId.HasValue && parent.Id > 0 &&
            child.ParentConditionId.Value == parent.Id);

    private static RTSRuleConditionDto ToConditionDto(
        RTSRuleConditionEntity node,
        IReadOnlyDictionary<RTSRuleConditionEntity, List<RTSRuleConditionEntity>> children,
        ISet<RTSRuleConditionEntity> ancestry)
    {
        if (!ancestry.Add(node))
            throw new InvalidOperationException("The rule contains a cyclic condition hierarchy.");

        var dto = new RTSRuleConditionDto
        {
            Id = node.Id > 0 ? node.Id : null,
            ParentConditionId = node.ParentConditionId,
            ConditionType = node.ConditionType,
            ConditionMatchType = node.ConditionMatchType,
            FieldDefinitionId = node.FieldDefinitionId,
            ComparisonOperator = node.ComparisonOperator,
            ExpectedValue = node.ExpectedValue,
            ConditionOrder = node.ConditionOrder,
            Children = children[node]
                .Select(child => ToConditionDto(child, children,
                    new HashSet<RTSRuleConditionEntity>(ancestry)))
                .ToList()
        };

        ancestry.Remove(node);
        return dto;
    }
}

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

    public async Task<PagedResult<RTSRuleServiceCardDto>> GetCardsAsync(
        RTSRuleServiceCardQueryParameters query,
        CancellationToken ct = default)
    {
        var selectedGroup = query.RuleGroup.Trim().ToUpperInvariant();
        if (selectedGroup.Length == 0)
            throw new ArgumentException("RuleGroup is required.", nameof(query));

        if (selectedGroup is not ("FEE" or "USER_ALLOCATION"))
            return new PagedResult<RTSRuleServiceCardDto>([], 0, 1,
                query.PageSize == -1 ? 1 : query.PageSize);

        var cards =
            from service in _services.GetQueryable().AsNoTracking()
            join department in _departments.GetQueryable().AsNoTracking()
                on service.DepartmentId equals department.Id
            join rule in _rules.GetQueryable().AsNoTracking()
                on service.Id equals rule.ServiceId
            where rule.RuleGroup == selectedGroup
                && service.IsActive
                && department.IsActive
                && (!query.DepartmentId.HasValue || service.DepartmentId == query.DepartmentId.Value)
                && (!query.ServiceId.HasValue || service.Id == query.ServiceId.Value)
            group rule by new
            {
                service.Id,
                service.ServiceName,
                service.DepartmentId,
                department.DepartmentName
            }
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
            cards = cards.Where(card =>
                card.ServiceName.Contains(search) || card.DepartmentName.Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            cards = cards.Where(card => query.IsActive.Value
                ? card.ActiveRuleCount > 0
                : card.ActiveRuleCount < card.RuleCount);
        }

        var total = await cards.CountAsync(ct);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "departmentname" => descending
                ? cards.OrderByDescending(card => card.DepartmentName)
                    .ThenByDescending(card => card.ServiceName)
                : cards.OrderBy(card => card.DepartmentName).ThenBy(card => card.ServiceName),
            "rulecount" => descending
                ? cards.OrderByDescending(card => card.RuleCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.RuleCount).ThenBy(card => card.ServiceName),
            "activerulecount" => descending
                ? cards.OrderByDescending(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName),
            _ => descending
                ? cards.OrderByDescending(card => card.ServiceName)
                    .ThenByDescending(card => card.ServiceId)
                : cards.OrderBy(card => card.ServiceName).ThenBy(card => card.ServiceId)
        };
        var items = query.PageSize == -1
            ? await ordered.ToListAsync(ct)
            : await ordered.Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize).ToListAsync(ct);
        var size = query.PageSize == -1 ? Math.Max(1, total) : query.PageSize;
        return new PagedResult<RTSRuleServiceCardDto>(items, total, query.PageNumber, size);
    }
}

public sealed class RTSStageAllocationRuleService : IRTSStageAllocationRuleService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSApprovalFlowMasterEntity, int> _flows;
    private readonly IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> _allocations;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fields;
    private readonly IRepository<UserEntity, int> _users;

    public RTSStageAllocationRuleService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSApprovalFlowMasterEntity, int> flows,
        IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> allocations,
        IRepository<RTSFieldDefinitionEntity, int> fields,
        IRepository<UserEntity, int> users)
    {
        _rules = rules;
        _flows = flows;
        _allocations = allocations;
        _fields = fields;
        _users = users;
    }

    public async Task ValidateAsync(
        int serviceId,
        int? userId,
        string? actionValue,
        string? conditionMatchType,
        IReadOnlyList<RTSRuleConditionDto> conditions,
        CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0) throw new InvalidOperationException("Select an RTS service.");
        var predicates = RTSRuleConditionTree.GetPredicateConditions(conditionMatchType, conditions);
        var fieldIds = predicates.Select(x => x.FieldDefinitionId!.Value).Distinct().ToList();
        var fields = await _fields.GetQueryable().AsNoTracking()
            .Where(x => x.ServiceId == serviceId && x.IsActive &&
                !x.MarkedForDeletion && fieldIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (fields.Count != fieldIds.Count)
            throw new InvalidOperationException(
                "Every condition field must be active and belong to this service.");

        foreach (var condition in predicates)
        {
            var field = fields[condition.FieldDefinitionId!.Value];
            if (!RTSRuleConditionMatcher.IsValidOperator(
                field.FieldType, condition.ComparisonOperator!))
                throw new InvalidOperationException(
                    $"Operator {condition.ComparisonOperator} is not valid for {field.FieldLabel}.");
            if (!RTSRuleConditionMatcher.IsValidExpectedValue(
                    field.FieldType, condition.ExpectedValue!, condition.ComparisonOperator!, out var validationError))
                throw new InvalidOperationException(validationError);
        }

        var (stageId, stageOrder) = ParseStage(actionValue);
        var flow = await _flows.GetQueryable().AsNoTracking()
            .Include(x => x.ApprovalFlowStages)
            .Where(x => x.ServiceId == serviceId && x.IsActive)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (flow == null || !flow.ApprovalFlowStages.Any(x =>
            x.Id == stageId && x.StageOrder == stageOrder))
            throw new InvalidOperationException(
                "Select a stage in the service's active approval workflow.");
        if (!userId.HasValue || userId.Value <= 0 ||
            !await IsEligibleAsync(stageId, userId.Value, cancellationToken))
            throw new InvalidOperationException(
                "Select an active officer allocated to the chosen stage.");
    }

    public async Task<int> ResolveOfficerAsync(
        int serviceId,
        int approvalFlowId,
        int stageId,
        IReadOnlyDictionary<int, string> values,
        CancellationToken cancellationToken = default)
    {
        var stageBelongsToFlow = await _flows.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == approvalFlowId && x.ServiceId == serviceId && x.IsActive &&
                x.ApprovalFlowStages.Any(stage => stage.Id == stageId),
                cancellationToken);
        if (!stageBelongsToFlow)
            throw new InvalidOperationException(
                $"Stage {stageId} does not belong to the application's active approval flow.");

        var candidates = await _rules.GetQueryable().AsNoTracking()
            .Include(x => x.Conditions)
            .Where(x => x.ServiceId == serviceId && x.IsActive &&
                x.RuleGroup == "USER_ALLOCATION" &&
                x.RuleActionType == "STAGE_ALLOCATION")
            .ToListAsync(cancellationToken);
        var matches = candidates
            .Where(x => TargetsStage(x.ActionValue, stageId) &&
                RTSRuleConditionMatcher.Matches(x, values))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToList();
        if (matches.Count == 0)
        {
            var allocatedOfficers = await (
                from allocation in _allocations.GetQueryable().AsNoTracking()
                join user in _users.GetQueryable().AsNoTracking()
                    on allocation.UserId equals user.Id
                where allocation.ApprovalFlowStageId == stageId &&
                      allocation.UserId.HasValue &&
                      allocation.IsActive &&
                      user.IsActive &&
                      !user.MarkedForDeletion
                select new
                {
                    UserId = allocation.UserId.Value,
                    allocation.Priority
                })
                .ToListAsync(cancellationToken);

            var fallbackCandidates = allocatedOfficers
                .GroupBy(x => x.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Priority = group.Min(x => x.Priority ?? int.MaxValue)
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.UserId)
                .ToList();

            if (fallbackCandidates.Count == 0)
                throw new InvalidOperationException(
                    $"No active officer is allocated to stage {stageId}.");
            if (fallbackCandidates.Count > 1 &&
                fallbackCandidates[0].Priority == fallbackCandidates[1].Priority)
                throw new InvalidOperationException(
                    $"Multiple stage officers share the best allocation priority for stage {stageId}.");

            return fallbackCandidates[0].UserId;
        }
        if (matches.Count > 1 && matches[0].Priority == matches[1].Priority)
            throw new InvalidOperationException(
                $"Multiple allocation rules match stage {stageId} at priority {matches[0].Priority}.");

        var winner = matches[0];
        if (!winner.UserId.HasValue ||
            !await IsEligibleAsync(stageId, winner.UserId.Value, cancellationToken))
            throw new InvalidOperationException(
                $"The selected officer for allocation rule {winner.Id} is no longer valid for stage {stageId}.");
        return winner.UserId.Value;
    }

    public IReadOnlyDictionary<int, string> NormalizeValues(
        IEnumerable<RTSFieldValueEntity> values) =>
        values.Where(x => x.FieldDefinitionId > 0 && !x.MarkedForDeletion)
            .GroupBy(x => x.FieldDefinitionId)
            .ToDictionary(
                x => x.Key,
                x => x.Last().TextValue?.Trim()
                    ?? x.Last().NumberValue?.ToString(CultureInfo.InvariantCulture)
                    ?? x.Last().DateValue?.ToString("O", CultureInfo.InvariantCulture)
                    ?? x.Last().BooleanValue?.ToString()
                    ?? string.Empty);

    private async Task<bool> IsEligibleAsync(int stageId, int userId, CancellationToken ct) =>
        await _allocations.GetQueryable().AsNoTracking().AnyAsync(x =>
            x.ApprovalFlowStageId == stageId && x.UserId == userId && x.IsActive, ct) &&
        await _users.GetQueryable().AsNoTracking().AnyAsync(x =>
            x.Id == userId && x.IsActive && !x.MarkedForDeletion, ct);

    private static bool TargetsStage(string? actionValue, int stageId)
    {
        try
        {
            return ParseStage(actionValue).StageId == stageId;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static (int StageId, int StageOrder) ParseStage(string? actionValue)
    {
        try
        {
            using var document = JsonDocument.Parse(actionValue ?? string.Empty);
            var root = document.RootElement;
            var stageId = root.GetProperty("approvalFlowStageId").GetInt32();
            var order = root.GetProperty("stageOrder").GetInt32();
            if (stageId > 0 && order > 0) return (stageId, order);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or
            InvalidOperationException or KeyNotFoundException)
        {
        }

        throw new InvalidOperationException("Select a valid approval workflow stage.");
    }
}

public static class RTSRuleConditionTree
{
    public const int MaximumDepth = 5;
    public const int MaximumNodes = 100;

    /// <summary>Validates the expression and returns every predicate node.</summary>
    public static IReadOnlyList<RTSRuleConditionDto> GetPredicateConditions(
        string? rootMatchType,
        IReadOnlyCollection<RTSRuleConditionDto> conditions)
    {
        if (!IsMatchType(rootMatchType))
            throw new InvalidOperationException("Condition match type must be ALL or ANY.");
        if (conditions.Count == 0)
            throw new InvalidOperationException("At least one condition is required.");

        var predicates = new List<RTSRuleConditionDto>();
        var total = 0;

        void ValidateNodes(IEnumerable<RTSRuleConditionDto> nodes, int depth)
        {
            if (depth > MaximumDepth)
                throw new InvalidOperationException($"Condition groups cannot exceed {MaximumDepth} levels.");

            var ordered = nodes.OrderBy(x => x.ConditionOrder).ToList();
            if (ordered.Count == 0)
                throw new InvalidOperationException("Every condition group requires at least one child condition.");
            if (ordered.Any(x => x.ConditionOrder <= 0) ||
                ordered.Select(x => x.ConditionOrder).Distinct().Count() != ordered.Count)
                throw new InvalidOperationException("Condition order must be positive and unique within each group.");

            foreach (var node in ordered)
            {
                if (++total > MaximumNodes)
                    throw new InvalidOperationException($"A rule cannot contain more than {MaximumNodes} condition nodes.");

                var type = node.ConditionType?.Trim().ToUpperInvariant();
                if (type == "GROUP")
                {
                    if (!IsMatchType(node.ConditionMatchType) || node.FieldDefinitionId.HasValue ||
                        !string.IsNullOrWhiteSpace(node.ComparisonOperator) ||
                        !string.IsNullOrWhiteSpace(node.ExpectedValue))
                        throw new InvalidOperationException(
                            "A condition group requires ALL or ANY and cannot contain a field comparison.");
                    ValidateNodes(node.Children, depth + 1);
                    continue;
                }

                if (type != "PREDICATE" || !node.FieldDefinitionId.HasValue ||
                    node.FieldDefinitionId.Value <= 0 ||
                    string.IsNullOrWhiteSpace(node.ComparisonOperator) ||
                    string.IsNullOrWhiteSpace(node.ExpectedValue) ||
                    node.ExpectedValue.Trim().Length > 500 ||
                    !string.IsNullOrWhiteSpace(node.ConditionMatchType) || node.Children.Count != 0)
                    throw new InvalidOperationException(
                        "Every predicate requires a field, operator, and expected value, and cannot contain child conditions.");

                predicates.Add(node);
            }
        }

        ValidateNodes(conditions, 1);
        return predicates;
    }

    public static bool IsMatchType(string? value) =>
        value?.Trim().ToUpperInvariant() is "ALL" or "ANY";
}

public static class RTSRuleConditionMatcher
{
    private const string DatePartsPrefix = "DATE_PARTS:";
    private const string DateTimePartsPrefix = "DATETIME_PARTS:";
    private const string DateAgeDaysPrefix = "DATE_AGE_DAYS:";
    private static readonly HashSet<string> DateAgeDaysOperators = new(StringComparer.Ordinal)
    {
        "DATE_AGE_DAYS_LT",
        "DATE_AGE_DAYS_LTE",
        "DATE_AGE_DAYS_GT",
        "DATE_AGE_DAYS_GTE",
        "DATE_AGE_DAYS_BETWEEN"
    };

    public static bool Matches(
        RTSRuleMasterEntity rule,
        IReadOnlyDictionary<int, string> values,
        DateTime? referenceDate = null)
        => MatchWithEvidence(rule, values, referenceDate).IsMatch;

    public static (bool IsMatch, List<RTSRuleConditionEntity> Predicates) MatchWithEvidence(
        RTSRuleMasterEntity rule,
        IReadOnlyDictionary<int, string> values,
        DateTime? referenceDate = null)
    {
        const int RootConditionKey = 0;
        var byParent = rule.Conditions
            .Where(x => x.IsActive)
            .GroupBy(x => x.ParentConditionId ?? RootConditionKey)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.ConditionOrder).ToList());

        (bool IsMatch, List<RTSRuleConditionEntity> Predicates) EvaluateChildren(int? parentId, string? matchType)
        {
            if (!byParent.TryGetValue(parentId ?? RootConditionKey, out var children) || children.Count == 0)
                return (false, []);

            var results = children.Select(EvaluateNode).ToList();
            var isMatch = string.Equals(matchType, "ANY", StringComparison.OrdinalIgnoreCase)
                ? results.Any(x => x.IsMatch)
                : results.All(x => x.IsMatch);
            return isMatch
                ? (true, results.Where(x => x.IsMatch).SelectMany(x => x.Predicates).ToList())
                : (false, []);
        }

        (bool IsMatch, List<RTSRuleConditionEntity> Predicates) EvaluateNode(RTSRuleConditionEntity node)
        {
            if (string.Equals(node.ConditionType, "GROUP", StringComparison.OrdinalIgnoreCase))
                return EvaluateChildren(node.Id, node.ConditionMatchType);

            var isMatch = string.Equals(node.ConditionType, "PREDICATE", StringComparison.OrdinalIgnoreCase) &&
                node.FieldDefinitionId.HasValue &&
                !string.IsNullOrWhiteSpace(node.ComparisonOperator) &&
                !string.IsNullOrWhiteSpace(node.ExpectedValue) &&
                values.TryGetValue(node.FieldDefinitionId.Value, out var actual) &&
                !string.IsNullOrWhiteSpace(actual) &&
                Compare(actual, node.ExpectedValue!, node.ComparisonOperator!, referenceDate);
            return isMatch ? (true, [node]) : (false, []);
        }

        return EvaluateChildren(null, rule.ConditionMatchType);
    }

    public static bool IsValidOperator(string fieldType, string comparisonOperator)
    {
        var type = NormalizeTemporalFieldType(fieldType);
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (DateAgeDaysOperators.Contains(op)) return type == "DATE";
        if (op is "=" or "!=") return true;
        if (op == "CONTAINS")
            return type is not ("NUMBER" or "DECIMAL" or "INTEGER" or "INT" or
                "CURRENCY" or "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or
                "TIME" or "MONTH");
        return op is ">" or ">=" or "<" or "<=" &&
            type is "NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY" or
                "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or "TIME" or "MONTH";
    }

    /// <summary>
    /// Validates feature-specific expected-value encodings before a rule is persisted.
    /// Date component rules use DATE_PARTS:day-month-year, where * means unspecified.
    /// Datetime component rules use DATETIME_PARTS:day-month-year|HH:mm, where * means unspecified.
    /// </summary>
    public static bool IsValidExpectedValue(
        string fieldType,
        string expectedValue,
        string comparisonOperator,
        out string validationError)
    {
        validationError = string.Empty;
        var normalizedFieldType = NormalizeTemporalFieldType(fieldType);
        var normalizedOperator = comparisonOperator.Trim().ToUpperInvariant();
        var isDateAgeDays = expectedValue.StartsWith(DateAgeDaysPrefix, StringComparison.OrdinalIgnoreCase);
        if (DateAgeDaysOperators.Contains(normalizedOperator))
        {
            if (normalizedFieldType != "DATE")
            {
                validationError = "Days-since-date conditions are supported only for Date fields.";
                return false;
            }

            if (TryParseDateAgeDays(expectedValue, normalizedOperator, out _)) return true;

            validationError = normalizedOperator == "DATE_AGE_DAYS_BETWEEN"
                ? "Enter whole-number minimum and maximum days, with the minimum no greater than the maximum."
                : "Enter a whole-number day value of zero or greater.";
            return false;
        }

        if (isDateAgeDays)
        {
            validationError = "The days-since-date value must use a days-since-date comparison operator.";
            return false;
        }

        var isDateParts = expectedValue.StartsWith(DatePartsPrefix, StringComparison.OrdinalIgnoreCase);
        var isDateTimeParts = expectedValue.StartsWith(DateTimePartsPrefix, StringComparison.OrdinalIgnoreCase);
        if (!isDateParts && !isDateTimeParts)
            return true;

        if ((isDateParts && normalizedFieldType != "DATE") ||
            (isDateTimeParts && normalizedFieldType != "DATETIME"))
        {
            validationError = "The expected value encoding does not match the selected field type.";
            return false;
        }

        if (comparisonOperator.Trim() is not ("=" or "!="))
        {
            validationError = "Partial date and datetime conditions support only Equal to or Not equal to.";
            return false;
        }

        if ((isDateParts && TryParseDateParts(expectedValue, out _)) ||
            (isDateTimeParts && TryParseDateTimeParts(expectedValue, out _)))
            return true;

        validationError = isDateTimeParts
            ? "Enter at least one valid datetime component: day, month, year, or time."
            : "Enter at least one valid date component: day, month, or year.";
        return false;
    }

    public static bool Compare(
        string actual,
        string expected,
        string comparisonOperator,
        DateTime? referenceDate = null)
    {
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (DateAgeDaysOperators.Contains(op))
        {
            if (!TryParseDateAgeDays(expected, op, out var days) ||
                !TryParseDate(actual, out var applicationDate))
                return false;

            var elapsedDays = ((referenceDate?.Date ?? GetIndianLocalDate()) - applicationDate.Date).Days;
            if (elapsedDays < 0) return false;

            return op == "DATE_AGE_DAYS_BETWEEN"
                ? elapsedDays >= days.Minimum && elapsedDays <= days.Maximum!.Value
                : CompareOrdered(elapsedDays, days.Minimum, DateAgeDaysComparisonOperator(op));
        }
        if (expected.StartsWith(DatePartsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (op is not ("=" or "!=") || !TryParseDateParts(expected, out var parts) ||
                !TryParseDate(actual, out var applicationDate))
                return false;

            var matches = (!parts.Day.HasValue || applicationDate.Day == parts.Day.Value) &&
                          (!parts.Month.HasValue || applicationDate.Month == parts.Month.Value) &&
                          (!parts.Year.HasValue || applicationDate.Year == parts.Year.Value);
            return op == "=" ? matches : !matches;
        }
        if (expected.StartsWith(DateTimePartsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (op is not ("=" or "!=") || !TryParseDateTimeParts(expected, out var parts) ||
                !TryParseDate(actual, out var dateTime))
                return false;

            var matches = (!parts.Day.HasValue || dateTime.Day == parts.Day.Value) &&
                          (!parts.Month.HasValue || dateTime.Month == parts.Month.Value) &&
                          (!parts.Year.HasValue || dateTime.Year == parts.Year.Value) &&
                          (!parts.Time.HasValue || dateTime.TimeOfDay == parts.Time.Value);
            return op == "=" ? matches : !matches;
        }
        if (op == "CONTAINS")
            return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
        if (op is "=" or "!=")
        {
            var equal = ValuesAreEqual(actual, expected);
            return op == "=" ? equal : !equal;
        }
        if (decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var number) &&
            decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var expectedNumber))
            return CompareOrdered(number, expectedNumber, op);
        if (DateTime.TryParse(actual, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date) &&
            DateTime.TryParse(expected, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var expectedDate))
            return CompareOrdered(date, expectedDate, op);
        return false;
    }

    private static bool TryParseDateAgeDays(
        string expected,
        string comparisonOperator,
        out DateAgeDays days)
    {
        days = default;
        if (!expected.StartsWith(DateAgeDaysPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var values = expected[DateAgeDaysPrefix.Length..]
            .Split(':', StringSplitOptions.TrimEntries);
        if (!int.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var minimum) ||
            minimum < 0)
            return false;

        if (comparisonOperator == "DATE_AGE_DAYS_BETWEEN")
        {
            if (values.Length != 2 ||
                !int.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var maximum) ||
                maximum < minimum)
                return false;

            days = new DateAgeDays(minimum, maximum);
            return true;
        }

        if (values.Length != 1) return false;
        days = new DateAgeDays(minimum, null);
        return true;
    }

    private static string DateAgeDaysComparisonOperator(string comparisonOperator) => comparisonOperator switch
    {
        "DATE_AGE_DAYS_LT" => "<",
        "DATE_AGE_DAYS_LTE" => "<=",
        "DATE_AGE_DAYS_GT" => ">",
        "DATE_AGE_DAYS_GTE" => ">=",
        _ => string.Empty
    };

    internal static DateTime GetIndianLocalDate()
    {
        foreach (var timeZoneId in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).Date;
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the platform-specific alternative.
            }
            catch (InvalidTimeZoneException)
            {
                // Fall back to UTC if this host has an invalid timezone entry.
            }
        }

        return DateTime.UtcNow.Date;
    }

    private static bool TryParseDateParts(string expected, out DateParts parts)
    {
        parts = default;
        if (!expected.StartsWith(DatePartsPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var tokens = expected[DatePartsPrefix.Length..].Split('-', StringSplitOptions.TrimEntries);
        if (tokens.Length != 3 ||
            !TryParseOptionalPart(tokens[0], 1, 31, out var day) ||
            !TryParseOptionalPart(tokens[1], 1, 12, out var month) ||
            !TryParseOptionalPart(tokens[2], 1, 9999, out var year) ||
            (!day.HasValue && !month.HasValue && !year.HasValue))
            return false;

        // A year-independent 29 February is valid, but impossible dates are not.
        if (day.HasValue && month.HasValue &&
            day.Value > DateTime.DaysInMonth(year ?? 2000, month.Value))
            return false;

        parts = new DateParts(day, month, year);
        return true;
    }

    private static bool TryParseOptionalPart(string token, int minimum, int maximum, out int? value)
    {
        value = null;
        if (token == "*") return true;
        if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < minimum || parsed > maximum)
            return false;
        value = parsed;
        return true;
    }

    private static bool TryParseDateTimeParts(string expected, out DateTimeParts parts)
    {
        parts = default;
        if (!expected.StartsWith(DateTimePartsPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var tokens = expected[DateTimePartsPrefix.Length..].Split('|', StringSplitOptions.TrimEntries);
        var dateTokens = tokens.Length == 2
            ? tokens[0].Split('-', StringSplitOptions.TrimEntries)
            : Array.Empty<string>();
        if (dateTokens.Length != 3 ||
            !TryParseOptionalPart(dateTokens[0], 1, 31, out var day) ||
            !TryParseOptionalPart(dateTokens[1], 1, 12, out var month) ||
            !TryParseOptionalPart(dateTokens[2], 1, 9999, out var year))
            return false;

        TimeSpan? time = null;
        TimeSpan parsedTime = default;
        if (tokens[1] != "*" && !TimeSpan.TryParseExact(tokens[1], @"hh\:mm", CultureInfo.InvariantCulture, out parsedTime))
            return false;
        if (tokens[1] != "*") time = parsedTime;

        if (!day.HasValue && !month.HasValue && !year.HasValue && !time.HasValue)
            return false;
        if (day.HasValue && month.HasValue &&
            day.Value > DateTime.DaysInMonth(year ?? 2000, month.Value))
            return false;

        parts = new DateTimeParts(day, month, year, time);
        return true;
    }

    private static string NormalizeTemporalFieldType(string? fieldType) =>
        fieldType?.Trim().Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant() switch
        {
            "DATETIME" or "DATETIMELOCAL" => "DATETIME",
            "DATE" => "DATE",
            _ => fieldType?.Trim().ToUpperInvariant() ?? string.Empty
        };

    private static bool TryParseDate(string value, out DateTime date)
    {
        var formats = new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "dd/MM/yyyy", "d/M/yyyy" };
        return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
                   DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out date) ||
               DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date);
    }

    private readonly record struct DateParts(int? Day, int? Month, int? Year);
    private readonly record struct DateTimeParts(int? Day, int? Month, int? Year, TimeSpan? Time);
    private readonly record struct DateAgeDays(int Minimum, int? Maximum);

    private static bool ValuesAreEqual(string actual, string expected)
    {
        if (string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var number) &&
            decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var expectedNumber))
            return number == expectedNumber;

        // Dynamic-form date controls persist ISO values while rule configuration
        // may use a date-only value. Treat equivalent calendar dates as equal.
        if (DateTime.TryParse(actual, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var date) &&
            DateTime.TryParse(expected, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var expectedDate))
        {
            // Preserve time for time/datetime fields while retaining date-only
            // equivalence for date controls that submit an ISO calendar date.
            var includesTime = actual.Contains(':') || expected.Contains(':') ||
                               actual.Contains('T') || expected.Contains('T');
            return includesTime ? date == expectedDate : date.Date == expectedDate.Date;
        }

        return false;
    }

    private static bool CompareOrdered<T>(T actual, T expected, string op)
        where T : IComparable<T> => op switch
        {
            ">" => actual.CompareTo(expected) > 0,
            ">=" => actual.CompareTo(expected) >= 0,
            "<" => actual.CompareTo(expected) < 0,
            "<=" => actual.CompareTo(expected) <= 0,
            _ => false
        };
}
