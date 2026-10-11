using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

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

    public async Task ValidateAsync(int serviceId, int? userId, string? actionValue,
        string? conditionMatchType, IReadOnlyList<RTSRuleConditionDto> conditions,
        CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0) throw new InvalidOperationException("Select an RTS service.");
        if (conditionMatchType?.Trim().ToUpperInvariant() is not ("ALL" or "ANY"))
            throw new InvalidOperationException("Condition match type must be ALL or ANY.");
        if (conditions.Count == 0) throw new InvalidOperationException("At least one condition is required.");
        if (conditions.Any(x => x.FieldDefinitionId <= 0 || string.IsNullOrWhiteSpace(x.ExpectedValue) || x.ExpectedValue.Length > 500))
            throw new InvalidOperationException("Every condition requires a field and expected value (up to 500 characters).");
        var fieldIds = conditions.Select(x => x.FieldDefinitionId).Distinct().ToList();
        var fields = await _fields.GetQueryable().AsNoTracking()
            .Where(x => x.ServiceId == serviceId && x.IsActive && !x.MarkedForDeletion && fieldIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (fields.Count != fieldIds.Count)
            throw new InvalidOperationException("Every condition field must be active and belong to this service.");
        foreach (var condition in conditions)
            if (!RTSRuleConditionMatcher.IsValidOperator(fields[condition.FieldDefinitionId].FieldType, condition.ComparisonOperator))
                throw new InvalidOperationException($"Operator {condition.ComparisonOperator} is not valid for {fields[condition.FieldDefinitionId].FieldLabel}.");

        var (stageId, stageOrder) = ParseStage(actionValue);
        var flow = await _flows.GetQueryable().AsNoTracking().Include(x => x.ApprovalFlowStages)
            .Where(x => x.ServiceId == serviceId && x.IsActive)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (flow == null || !flow.ApprovalFlowStages.Any(x => x.Id == stageId && x.StageOrder == stageOrder && x.IsActive))
            throw new InvalidOperationException("Select a stage in the service's active approval workflow.");
        if (!userId.HasValue || userId.Value <= 0 || !await IsEligibleAsync(stageId, userId.Value, cancellationToken))
            throw new InvalidOperationException("Select an active officer allocated to the chosen stage.");
    }

    public async Task<int> ResolveOfficerAsync(int serviceId, int approvalFlowId, int stageId,
        int defaultUserId, IReadOnlyDictionary<int, string> values, CancellationToken cancellationToken = default)
    {
        var candidates = await _rules.GetQueryable().AsNoTracking().Include(x => x.Conditions)
            .Where(x => x.ServiceId == serviceId && x.IsActive &&
                x.RuleGroup == "USER_ALLOCATION" && x.RuleActionType == "STAGE_ALLOCATION")
            .ToListAsync(cancellationToken);
        var matches = candidates.Where(x => TargetsStage(x.ActionValue, stageId) &&
                RTSRuleConditionMatcher.Matches(x, values))
            .OrderBy(x => x.Priority).ThenBy(x => x.Id).ToList();
        if (matches.Count == 0) return defaultUserId;
        if (matches.Count > 1 && matches[0].Priority == matches[1].Priority)
            throw new InvalidOperationException($"Multiple allocation rules match stage {stageId} at priority {matches[0].Priority}.");
        var winner = matches[0];
        var stageBelongsToFlow = await _flows.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == approvalFlowId && x.ServiceId == serviceId &&
                x.ApprovalFlowStages.Any(stage => stage.Id == stageId), cancellationToken);
        if (!stageBelongsToFlow || !winner.UserId.HasValue ||
            !await IsEligibleAsync(stageId, winner.UserId.Value, cancellationToken))
            throw new InvalidOperationException($"The selected officer for allocation rule {winner.Id} is no longer valid for stage {stageId}.");
        return winner.UserId.Value;
    }

    public IReadOnlyDictionary<int, string> NormalizeValues(IEnumerable<RTSFieldValueEntity> values) =>
        values.Where(x => x.FieldDefinitionId > 0 && !x.MarkedForDeletion)
            .GroupBy(x => x.FieldDefinitionId)
            .ToDictionary(x => x.Key, x => x.Last().TextValue?.Trim()
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
        try { return ParseStage(actionValue).StageId == stageId; }
        catch (InvalidOperationException) { return false; }
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
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException) { }
        throw new InvalidOperationException("Select a valid approval workflow stage.");
    }
}
