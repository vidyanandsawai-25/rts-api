using System.Dynamic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleCategory;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using RulesEngine.Models;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Executes Microsoft Rules Engine policies stored in <c>RuleEngineMaster.RuleJson</c>
    /// against a dynamic property tax input and returns the matched rule effects.
    ///
    /// <para>
    /// <b>Execution pipeline:</b><br/>
    /// 1. Load active rules for the requested category, ordered by Priority (ascending).<br/>
    /// 2. Parse each rule's stored JSON into an MS Rules Engine <see cref="Workflow"/>.<br/>
    /// 3. Execute each workflow against the dynamic input.<br/>
    /// 4. On a match: build a <see cref="RuleExecutionResultDto"/>,
    ///    and honour the StopProcessing flag.
    /// </para>
    /// </summary>
    public class RuleExecutionService : IRuleExecutionService
    {
        private readonly IRepository<RuleEngineEntity, int> _ruleRepository;
        private readonly IRepository<RuleCategoryEntity, int> _categoryRepository;
        private readonly IEnumerable<IRuleEffectApplicator> _effectApplicators;
        private readonly ILogger<RuleExecutionService> _logger;

        public RuleExecutionService(
            IRepository<RuleEngineEntity, int> ruleRepository,
            IRepository<RuleCategoryEntity, int> categoryRepository,
            IEnumerable<IRuleEffectApplicator> effectApplicators,
            ILogger<RuleExecutionService> logger)
        {
            _ruleRepository = ruleRepository;
            _categoryRepository = categoryRepository;
            _effectApplicators = effectApplicators;
            _logger = logger;
        }

        // ─── Public API ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<RuleCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _categoryRepository
                .GetQueryable()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return categories.Select(c => new RuleCategoryDto
            {
                Value = c.CategoryCode,
                Label = c.CategoryName,
                SortOrder = c.SortOrder
            }).ToList();
        }

        /// <inheritdoc/>
        public async Task<List<RuleExecutionResultDto>> ExecuteAsync(
            RuleExecutionInputDto input,
            CancellationToken cancellationToken = default)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (string.IsNullOrWhiteSpace(input.Category))
                throw new ArgumentException("Category is required.", nameof(input));

            if (input.Input == null || !input.Input.Any())
                throw new ArgumentException("Input dictionary cannot be null or empty.", nameof(input));

            _logger.LogInformation("Executing rules for Category={Category}", input.Category);

            // ── Step 1: Load active rules ordered by Priority (DB row level) then Id ──
            // All rules are Property-Level rules. Priority on RuleEngineEntity controls
            // which DB row (and its contained RuleJson sub-rules) executes first.
            // RuleScopeId is retained on the entity for future use but is not used here.
            var ruleEntities = await _ruleRepository.GetQueryable()
                .Include(r => r.RuleScope)
                .Where(r => r.RuleCategory == input.Category && r.IsEnabled && r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!ruleEntities.Any())
                return new List<RuleExecutionResultDto>();

            // ── Step 2: Parse DB rule entities into MS Rules Engine Workflow objects ───────────
            var ruleWorkflows = ParseRuleEntitiesToWorkflows(ruleEntities);

            if (!ruleWorkflows.Any())
                return new List<RuleExecutionResultDto>();

            // ── Step 3: Build the engine and the typed dynamic input ─────────────────────────
            // RuleParameter named "input" makes expressions like "input.Floor == 65" work.
            var rulesEngine = new global::RulesEngine.RulesEngine(ruleWorkflows.Select(w => w.Workflow).ToArray());
            var dynamicInput = BuildDynamicInput(input.Input);
            var engineParameter = new RuleParameter("input", dynamicInput);

            // ── Step 4: Evaluate each workflow with stop-processing logic ────────
            var appliedRuleIds = new HashSet<int>();
            var results = new List<RuleExecutionResultDto>();

            foreach (var (workflow, ruleEffectsMap, ruleStopProcessingMap, ruleId, stopOnMatch, ruleOrderIndex, entity) in ruleWorkflows)
            {
                try
                {
                    var ruleResults = await rulesEngine.ExecuteAllRulesAsync(
                        workflow.WorkflowName,
                        new[] { engineParameter });

                    // ── Sort results by original JSON array position ──────────────────────────
                    // MS Rules Engine does NOT guarantee return order matches the Rules[] list order.
                    // We sort here so stopProcessing halts at the correct rule (e.g. rule[1] stops
                    // before rule[2] is processed, even if the engine returned them out of order).
                    var orderedResults = ruleResults
                        .OrderBy(r => ruleOrderIndex.TryGetValue(r.Rule.RuleName, out var idx) ? idx : int.MaxValue);

                    foreach (var ruleResult in orderedResults)
                    {
                        if (!ruleResult.IsSuccess)
                            continue;

                        // Rule matched — look up the effect JSON from our side dictionary
                        var effectsJson = ruleEffectsMap.TryGetValue(ruleResult.Rule.RuleName, out var matchedEffectJson)
                            ? (JsonElement?)matchedEffectJson
                            : null;

                        var result = await BuildRuleResultAsync(
                            ruleResult.Rule.RuleName,
                            ruleResult.Rule.ErrorMessage,
                            effectsJson,
                            input.Input);
                        if (result == null)
                            continue;

                        var ruleStop = ruleStopProcessingMap.TryGetValue(ruleResult.Rule.RuleName, out var shouldStopProcessing) && shouldStopProcessing;
                        var shouldStop = stopOnMatch || ruleStop;

                        result.StopProcessing = shouldStop;
                        result.RuleScopeId = entity.RuleScopeId;
                        result.RuleScopeName = entity.RuleScope != null ? entity.RuleScope.RuleScope : null;
                        results.Add(result);
                        appliedRuleIds.Add(ruleId);

                        if (shouldStop)
                        {
                            _logger.LogInformation(
                                "Rule '{RuleCode}' (Id={RuleId}) triggered stop processing. " +
                                "Halting execution for Category={Category}. {MatchCount} rule(s) matched.",
                                workflow.WorkflowName, ruleId, input.Category, results.Count);
                            return results;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                         "Exception executing workflow '{WorkflowName}' for Category={Category}.",
                        workflow.WorkflowName, input.Category);
                }
            }

            _logger.LogInformation(
                "Execution complete for Category={Category}. {MatchCount}/{TotalRules} rules matched.",
                input.Category, results.Count, ruleWorkflows.Count);

            return results;
        }

        /// <inheritdoc/>
        public async Task<RuleDryRunResultDto> DryRunAsync(
            RuleDryRunInputDto input,
            CancellationToken cancellationToken = default)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Input == null || !input.Input.Any())
                throw new ArgumentException("Input dictionary cannot be null or empty.", nameof(input));

            var dryRunResult = new RuleDryRunResultDto
            {
                Category = input.Category,
                ResolvedInput = input.Input.ToDictionary(k => k.Key, k => k.Value?.ToString() ?? "null")
            };

            // ── Step 1: Resolve rule entities — from DB or from raw RuleJson ────────
            List<RuleEngineEntity> ruleEntities;

            if (!string.IsNullOrWhiteSpace(input.RuleJson))
            {
                // Ad-hoc test mode: wrap the supplied JSON into a synthetic entity
                ruleEntities = new List<RuleEngineEntity>
                {
                    new RuleEngineEntity
                    {
                        Id         = 0,
                        RuleCode   = "DRY-RUN-ADHOC",
                        RuleName   = "Ad-hoc Dry Run",
                        RuleJson   = input.RuleJson,
                        RuleCategory = input.Category,
                        Priority   = 1,
                        IsEnabled  = true,
                        IsActive   = true,
                        StopProcessing = false
                    }
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input.Category))
                    throw new ArgumentException("Category is required when RuleJson is not provided.", nameof(input));

                ruleEntities = await _ruleRepository.GetQueryable()
                    .Where(r => r.RuleCategory == input.Category && r.IsEnabled && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.Id)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }

            dryRunResult.TotalRulesLoaded = ruleEntities.Count;

            if (!ruleEntities.Any())
                return dryRunResult;

            // ── Step 2: Build the engine input ──────────────────────────────────────
            // If the caller provided an explicit BaseValue, inject it into the input
            // dictionary so ResolveBaseRate finds it via the standard fallback keys.
            // We inject under all three well-known fallback keys so any ParameterCode works.
            var effectiveInput = new Dictionary<string, object>(input.Input);
            if (input.BaseValue.HasValue)
            {
                effectiveInput["Rate"] = input.BaseValue.Value;
                effectiveInput["BaseRate"] = input.BaseValue.Value;
                effectiveInput["RatePerSqMt"] = input.BaseValue.Value;

                // Also keep it visible in the resolved-input snapshot
                dryRunResult.ResolvedInput["BaseValue (override)"] = input.BaseValue.Value.ToString("G");
            }

            var dynamicInput = BuildDynamicInput(effectiveInput);
            var engineParameter = new RuleParameter("input", dynamicInput);

            // ── Step 3: Evaluate each entity and collect the full trace ─────────────
            foreach (var entity in ruleEntities)
            {
                var workflowResult = await EvaluateDryRunEntityAsync(
                    entity, engineParameter, effectiveInput, cancellationToken);

                dryRunResult.Workflows.Add(workflowResult);
                dryRunResult.TotalSubRulesEvaluated += workflowResult.SubRules.Count;
                dryRunResult.MatchedCount += workflowResult.SubRules.Count(r => r.IsMatch && !r.WasSkipped);

                // Check if any matched sub-rule triggered stop processing
                var stopTriggered = workflowResult.SubRules
                    .Where(r => r.IsMatch && !r.WasSkipped)
                    .Any(r => r.StopProcessing || workflowResult.EntityStopOnMatch);

                if (stopTriggered)
                {
                    dryRunResult.StoppedEarly = true;
                    break;
                }
            }

            return dryRunResult;
        }

        /// <summary>
        /// Evaluates all sub-rules within a single <see cref="RuleEngineEntity"/> and returns
        /// a per-workflow dry-run trace containing both matched and unmatched sub-rule results.
        /// </summary>
        private async Task<RuleDryRunWorkflowResult> EvaluateDryRunEntityAsync(
            RuleEngineEntity entity,
            RuleParameter engineParameter,
            Dictionary<string, object> inputValues,
            CancellationToken cancellationToken)
        {
            var workflowResult = new RuleDryRunWorkflowResult
            {
                RuleEntityId = entity.Id,
                Priority = entity.Priority,
                EntityStopOnMatch = entity.StopProcessing,
                WorkflowName = entity.RuleCode
            };

            if (string.IsNullOrWhiteSpace(entity.RuleJson))
            {
                workflowResult.WorkflowName = entity.RuleCode + " [empty RuleJson]";
                return workflowResult;
            }

            try
            {
                var ruleJson = JsonSerializer.Deserialize<JsonElement>(entity.RuleJson);

                if (ruleJson.TryGetProperty("RuleName", out var ruleNameProp))
                    workflowResult.WorkflowName = ruleNameProp.GetString() ?? entity.RuleCode;

                if (!ruleJson.TryGetProperty("rules", out var rulesArray) ||
                    rulesArray.ValueKind != JsonValueKind.Array)
                    return workflowResult;

                // Parse sub-rules — tracking every sub-rule even skipped ones
                var rules = new List<Rule>();
                var ruleEffectsMap = new Dictionary<string, JsonElement>();
                var ruleStopMap = new Dictionary<string, bool>();
                var ruleOrderIndex = new Dictionary<string, int>();
                var skippedSubRules = new List<RuleDryRunSubRuleResult>();

                int arrayPos = 0;
                foreach (var subRuleElement in rulesArray.EnumerateArray())
                {
                    var subRuleTrace = ParseSubRuleForDryRun(
                        subRuleElement, entity, arrayPos,
                        rules, ruleEffectsMap, ruleStopMap, ruleOrderIndex);
                    arrayPos++;

                    if (subRuleTrace.WasSkipped)
                        skippedSubRules.Add(subRuleTrace);
                }

                if (!rules.Any())
                {
                    // All sub-rules were skipped — add them to trace and return
                    workflowResult.SubRules.AddRange(skippedSubRules);
                    return workflowResult;
                }

                // Execute the workflow
                var workflow = new Workflow { WorkflowName = workflowResult.WorkflowName, Rules = rules };
                var rulesEngine = new global::RulesEngine.RulesEngine(new[] { workflow });

                var ruleResults = await rulesEngine.ExecuteAllRulesAsync(
                    workflow.WorkflowName,
                    new[] { engineParameter });

                // Re-sort by original JSON order
                var orderedResults = ruleResults
                    .OrderBy(r => ruleOrderIndex.TryGetValue(r.Rule.RuleName, out var idx) ? idx : int.MaxValue);

                // Build sub-rule trace entries for all evaluated rules
                var evaluatedTrace = new Dictionary<string, RuleDryRunSubRuleResult>();

                foreach (var ruleResult in orderedResults)
                {
                    var idx = ruleOrderIndex.TryGetValue(ruleResult.Rule.RuleName, out var i) ? i : -1;

                    var subTrace = new RuleDryRunSubRuleResult
                    {
                        ArrayIndex = idx,
                        RuleCode = ruleResult.Rule.RuleName,
                        RuleName = ruleResult.Rule.ErrorMessage ?? ruleResult.Rule.RuleName,
                        Expression = ruleResult.Rule.Expression,
                        IsMatch = ruleResult.IsSuccess,
                        StopProcessing = ruleStopMap.TryGetValue(ruleResult.Rule.RuleName, out var sp) && sp,
                        MatchStatus = ruleResult.IsSuccess
                            ? "Matched"
                            : (!string.IsNullOrWhiteSpace(ruleResult.ExceptionMessage)
                                ? $"Not matched: {ruleResult.ExceptionMessage}"
                                : "Not matched: condition evaluated to false")
                    };

                    // If matched, extract effect details and compute the actual rate
                    if (ruleResult.IsSuccess &&
                        ruleEffectsMap.TryGetValue(ruleResult.Rule.RuleName, out var effectJson))
                    {
                        subTrace.Effect = ExtractDryRunEffect(effectJson, ruleResult.Rule.RuleName);

                        // Reuse BuildRuleResultAsync to get the exact same BaseRate + ComputedRate
                        // as the Execute endpoint would produce — no duplication of applicator logic.
                        var computedResult = await BuildRuleResultAsync(
                            ruleResult.Rule.RuleName,
                            ruleResult.Rule.ErrorMessage ?? ruleResult.Rule.RuleName,
                            effectJson,
                            inputValues);

                        if (computedResult != null)
                        {
                            subTrace.BaseRate = computedResult.BaseRate;
                            subTrace.ComputedValue = computedResult.ComputedRate;
                        }
                    }

                    evaluatedTrace[ruleResult.Rule.RuleName] = subTrace;
                }

                // Merge: skipped first, then evaluated in array order
                var allSubRules = new List<RuleDryRunSubRuleResult>(skippedSubRules);
                allSubRules.AddRange(evaluatedTrace.Values.OrderBy(r => r.ArrayIndex));
                workflowResult.SubRules = allSubRules.OrderBy(r => r.ArrayIndex).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Dry-run evaluation failed for RuleCode='{RuleCode}'.", entity.RuleCode);
                workflowResult.WorkflowName = entity.RuleCode + $" [parse error: {ex.Message}]";
            }

            return workflowResult;
        }

        /// <summary>
        /// Parses a single sub-rule JSON element for dry-run evaluation.
        /// Adds valid rules to <paramref name="rules"/> and populates the side dictionaries.
        /// Returns a <see cref="RuleDryRunSubRuleResult"/> marked as skipped if the sub-rule
        /// is disabled, has no expression, or fails the safety check.
        /// </summary>
        private RuleDryRunSubRuleResult ParseSubRuleForDryRun(
            JsonElement subRuleElement,
            RuleEngineEntity entity,
            int arrayPos,
            List<Rule> rules,
            Dictionary<string, JsonElement> ruleEffectsMap,
            Dictionary<string, bool> ruleStopMap,
            Dictionary<string, int> ruleOrderIndex)
        {
            // Determine rule name
            var ruleName = entity.RuleCode;
            if (subRuleElement.TryGetProperty("RuleCode", out var ruleCodeProp) && !string.IsNullOrWhiteSpace(ruleCodeProp.GetString()))
                ruleName = ruleCodeProp.GetString()!;
            else if (subRuleElement.TryGetProperty("ruleName", out var ruleNameFallback) && !string.IsNullOrWhiteSpace(ruleNameFallback.GetString()))
                ruleName = ruleNameFallback.GetString()!;

            // Determine human-readable name
            var errorMessage = string.Empty;
            if (subRuleElement.TryGetProperty("errorMessage", out var errMsg) && !string.IsNullOrWhiteSpace(errMsg.GetString()))
                errorMessage = errMsg.GetString()!;
            else if (subRuleElement.TryGetProperty("ErrorMessage", out var errMsgAlt) && !string.IsNullOrWhiteSpace(errMsgAlt.GetString()))
                errorMessage = errMsgAlt.GetString()!;
            else if (subRuleElement.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
                errorMessage = desc.GetString()!;
            else if (subRuleElement.TryGetProperty("Description", out var descAlt) && !string.IsNullOrWhiteSpace(descAlt.GetString()))
                errorMessage = descAlt.GetString()!;

            if (string.IsNullOrWhiteSpace(errorMessage))
                errorMessage = !string.IsNullOrWhiteSpace(entity.Description) ? entity.Description : entity.RuleName ?? ruleName;

            var baseTrace = new RuleDryRunSubRuleResult
            {
                ArrayIndex = arrayPos,
                RuleCode = ruleName,
                RuleName = errorMessage
            };

            // Check: disabled flag
            if (subRuleElement.TryGetProperty("enabled", out var enabledProp) && !enabledProp.GetBoolean())
            {
                baseTrace.WasSkipped = true;
                baseTrace.SkipReason = "disabled (\"enabled\": false)";
                baseTrace.MatchStatus = "Skipped";
                return baseTrace;
            }

            // Check: missing expression
            if (!subRuleElement.TryGetProperty("expression", out var expressionProp))
            {
                baseTrace.WasSkipped = true;
                baseTrace.SkipReason = "missing \"expression\" field";
                baseTrace.MatchStatus = "Skipped";
                return baseTrace;
            }
            var expression = expressionProp.GetString();
            if (string.IsNullOrWhiteSpace(expression))
            {
                baseTrace.WasSkipped = true;
                baseTrace.SkipReason = "empty expression";
                baseTrace.MatchStatus = "Skipped";
                return baseTrace;
            }

            // Check: safety
            if (!IsExpressionSafe(expression))
            {
                baseTrace.WasSkipped = true;
                baseTrace.SkipReason = "blocked by security check (unsafe expression)";
                baseTrace.MatchStatus = "Skipped";
                baseTrace.Expression = expression;
                return baseTrace;
            }

            // Normalise and register
            var normalised = NormalizeLogicalOperators(expression);
            baseTrace.Expression = normalised;

            ruleOrderIndex[ruleName] = rules.Count;

            rules.Add(new Rule
            {
                RuleName = ruleName,
                Expression = normalised,
                RuleExpressionType = RuleExpressionType.LambdaExpression,
                ErrorMessage = errorMessage
            });

            // Store effects
            if (subRuleElement.TryGetProperty("Actions", out var actionsEl) &&
                actionsEl.ValueKind != JsonValueKind.Null)
                ruleEffectsMap[ruleName] = actionsEl;
            else if (!string.IsNullOrWhiteSpace(entity.EffectJson))
                ruleEffectsMap[ruleName] = WrapEffectJson(entity.EffectJson, ruleName);

            // Store stop flag
            var stopFlag = false;
            if (subRuleElement.TryGetProperty("stopProcessing", out var spProp))
                stopFlag = spProp.ValueKind == JsonValueKind.True;
            else if (subRuleElement.TryGetProperty("StopProcessing", out var spPropAlt))
                stopFlag = spPropAlt.ValueKind == JsonValueKind.True;
            ruleStopMap[ruleName] = stopFlag;

            return baseTrace; // will be updated with IsMatch after engine runs
        }

        /// <summary>
        /// Extracts effect details from an <c>Actions.OnSuccess.Context</c> JSON element
        /// for inclusion in the dry-run sub-rule trace.
        /// </summary>
        private static RuleDryRunEffect? ExtractDryRunEffect(JsonElement actionsJson, string ruleName)
        {
            try
            {
                if (!actionsJson.TryGetProperty("OnSuccess", out var onSuccess) ||
                    !onSuccess.TryGetProperty("Context", out var context))
                    return null;

                var contextDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(context.GetRawText())
                                  ?? new Dictionary<string, JsonElement>();

                return new RuleDryRunEffect
                {
                    EffectType = ReadStringFromContext(contextDict, "effectType"),
                    EffectValue = decimal.TryParse(ReadStringFromContext(contextDict, "value"), out var ev) ? ev : 0m,
                    ParameterCode = ReadStringFromContext(contextDict, "ParameterCode"),
                    Context = contextDict.ToDictionary(k => k.Key, k => k.Value.ToString())
                };
            }
            catch
            {
                return null;
            }
        }

        // ─── Private Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Parses a list of <see cref="RuleEngineEntity"/> records into MS Rules Engine
        /// <see cref="Workflow"/> objects, along with the associated effects map and metadata.
        ///
        /// <para>
        /// Return tuple members:<br/>
        /// — <c>Workflow</c>: the engine-ready workflow object (WorkflowName + Rules list).<br/>
        /// — <c>RuleEffectsMap</c>: maps each rule name → its raw <c>Actions</c> JSON element,
        ///   used to extract the effect type and value after a rule matches.<br/>
        /// — <c>RuleId</c>: the <see cref="RuleEngineEntity.Id"/> for exclusion tracking.<br/>
        /// — <c>StopOnMatch</c>: mirrors <see cref="RuleEngineEntity.StopProcessing"/>.
        /// </para>
        /// </summary>
        private List<(Workflow Workflow, Dictionary<string, JsonElement> RuleEffectsMap, Dictionary<string, bool> RuleStopProcessingMap, int RuleId, bool StopOnMatch, Dictionary<string, int> RuleOrderIndex, RuleEngineEntity Entity)>
            ParseRuleEntitiesToWorkflows(List<RuleEngineEntity> ruleEntities)
        {
            var ruleWorkflows = new List<(Workflow, Dictionary<string, JsonElement>, Dictionary<string, bool>, int, bool, Dictionary<string, int>, RuleEngineEntity)>();

            foreach (var entity in ruleEntities)
            {
                if (string.IsNullOrWhiteSpace(entity.RuleJson))
                    continue;

                try
                {
                    var ruleJson = JsonSerializer.Deserialize<JsonElement>(entity.RuleJson);

                    // WorkflowName = RuleCode (unique per entity); can be overridden by "RuleName" in JSON
                    var workflowName = entity.RuleCode;
                    if (ruleJson.TryGetProperty("RuleName", out var ruleNameJsonProp))
                        workflowName = ruleNameJsonProp.GetString() ?? workflowName;

                    if (!ruleJson.TryGetProperty("rules", out var rulesArrayJsonProp) ||
                        rulesArrayJsonProp.ValueKind != JsonValueKind.Array)
                        continue;

                    var rules = new List<Rule>();
                    var ruleEffectsMap = new Dictionary<string, JsonElement>();
                    var ruleStopProcessingMap = new Dictionary<string, bool>();
                    var ruleOrderIndex = new Dictionary<string, int>(); // tracks original array position

                    foreach (var subRuleElement in rulesArrayJsonProp.EnumerateArray())
                    {
                        // Determine rule name: prefer "RuleCode" in JSON, then "ruleName", then entity.RuleCode
                        var ruleName = entity.RuleCode;
                        if (subRuleElement.TryGetProperty("RuleCode", out var ruleCodeJsonProp) && !string.IsNullOrWhiteSpace(ruleCodeJsonProp.GetString()))
                            ruleName = ruleCodeJsonProp.GetString()!;
                        else if (subRuleElement.TryGetProperty("ruleName", out var ruleNameFallbackProp) && !string.IsNullOrWhiteSpace(ruleNameFallbackProp.GetString()))
                            ruleName = ruleNameFallbackProp.GetString()!;

                        // Expression is required
                        if (!subRuleElement.TryGetProperty("expression", out var expressionJsonProp))
                            continue;
                        var expression = expressionJsonProp.GetString();
                        if (string.IsNullOrWhiteSpace(expression))
                            continue;

                        // Skip disabled rules
                        if (subRuleElement.TryGetProperty("enabled", out var enabledFlagProp) && !enabledFlagProp.GetBoolean())
                            continue;

                        // Block potentially unsafe expressions before passing to the engine
                        if (!IsExpressionSafe(expression))
                        {
                            _logger.LogWarning(
                                "Security check failed for rule '{RuleName}'. Expression blocked: {Expression}",
                                ruleName, expression);
                            continue;
                        }

                        // Track the order this rule appears in the JSON array (0-based).
                        // Used later to sort ruleResults back into original array order,
                        // which guarantees stopProcessing halts at the correct rule.
                        ruleOrderIndex[ruleName] = rules.Count;

                        var errorMessage = string.Empty;
                        if (subRuleElement.TryGetProperty("errorMessage", out var errMsgProp) && !string.IsNullOrWhiteSpace(errMsgProp.GetString()))
                            errorMessage = errMsgProp.GetString()!;
                        else if (subRuleElement.TryGetProperty("ErrorMessage", out var errMsgPropAlt) && !string.IsNullOrWhiteSpace(errMsgPropAlt.GetString()))
                            errorMessage = errMsgPropAlt.GetString()!;
                        else if (subRuleElement.TryGetProperty("description", out var descProp) && !string.IsNullOrWhiteSpace(descProp.GetString()))
                            errorMessage = descProp.GetString()!;
                        else if (subRuleElement.TryGetProperty("Description", out var descPropAlt) && !string.IsNullOrWhiteSpace(descPropAlt.GetString()))
                            errorMessage = descPropAlt.GetString()!;

                        if (string.IsNullOrWhiteSpace(errorMessage))
                        {
                            errorMessage = !string.IsNullOrWhiteSpace(entity.Description)
                                ? entity.Description
                                : (!string.IsNullOrWhiteSpace(entity.RuleName) ? entity.RuleName : entity.RuleCode);
                        }

                        rules.Add(new Rule
                        {
                            RuleName = ruleName,
                            Expression = NormalizeLogicalOperators(expression),
                            RuleExpressionType = RuleExpressionType.LambdaExpression,
                            ErrorMessage = errorMessage
                            // Note: we do NOT set rule.Actions here.
                            // MS Rules Engine would try to invoke our effect names as registered IAction plugins.
                            // We handle effect application ourselves via IRuleEffectApplicator.
                        });

                        // Store the effect JSON in our own dictionary, keyed by rule name.
                        // Priority: Actions block inside RuleJson → EffectJson column on the entity.
                        if (subRuleElement.TryGetProperty("Actions", out var actionsJsonElement) &&
                            actionsJsonElement.ValueKind != JsonValueKind.Null)
                        {
                            ruleEffectsMap[ruleName] = actionsJsonElement;
                        }
                        else if (!string.IsNullOrWhiteSpace(entity.EffectJson))
                        {
                            ruleEffectsMap[ruleName] = WrapEffectJson(entity.EffectJson, ruleName);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "No Actions or EffectJson found for rule '{RuleName}'. Effect will be skipped.",
                                ruleName);
                        }

                        // Store the stopProcessing flag mapping
                        var ruleStopProcessing = false;
                        if (subRuleElement.TryGetProperty("stopProcessing", out var stopProcessingJsonProp))
                        {
                            if (stopProcessingJsonProp.ValueKind == JsonValueKind.True) ruleStopProcessing = true;
                            else if (stopProcessingJsonProp.ValueKind == JsonValueKind.False) ruleStopProcessing = false;
                        }
                        else if (subRuleElement.TryGetProperty("StopProcessing", out var stopProcessingJsonPropAlt))
                        {
                            if (stopProcessingJsonPropAlt.ValueKind == JsonValueKind.True) ruleStopProcessing = true;
                            else if (stopProcessingJsonPropAlt.ValueKind == JsonValueKind.False) ruleStopProcessing = false;
                        }
                        ruleStopProcessingMap[ruleName] = ruleStopProcessing;
                    }

                    if (!rules.Any())
                        continue;

                    ruleWorkflows.Add((
                        new Workflow { WorkflowName = workflowName, Rules = rules },
                        ruleEffectsMap,
                        ruleStopProcessingMap,
                        entity.Id,
                        entity.StopProcessing,
                        ruleOrderIndex,
                        entity
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to parse RuleJson for RuleCode='{RuleCode}'. Skipping this rule.",
                        entity.RuleCode);
                }
            }

            return ruleWorkflows;
        }

        /// <summary>
        /// Wraps a raw <c>EffectJson</c> string (from <see cref="RuleEngineEntity.EffectJson"/>)
        /// into the <c>Actions.OnSuccess.Context</c> envelope that <see cref="BuildRuleResultAsync"/> expects.
        /// </summary>
        private JsonElement WrapEffectJson(string effectJson, string ruleName)
        {
            try
            {
                var effect = JsonSerializer.Deserialize<JsonElement>(effectJson);
                var wrapped = new { OnSuccess = new { Context = effect } };
                return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(wrapped));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to parse EffectJson for rule '{RuleName}'.", ruleName);
                return default;
            }
        }

        /// <summary>
        /// After a rule condition matches, extracts the effect type and value from the
        /// stored <c>Actions.OnSuccess.Context</c> JSON, then applies the effect via
        /// the matching <see cref="IRuleEffectApplicator"/> to produce a computed rate.
        /// </summary>
        private async Task<RuleExecutionResultDto?> BuildRuleResultAsync(
            string ruleName,
            string ruleErrorMessage,
            JsonElement? effectsJson,
            Dictionary<string, object> inputValues)
        {
            try
            {
                if (effectsJson == null)
                    return null;

                var actionsJsonBlock = effectsJson.Value;

                // Navigate: Actions → OnSuccess → Context
                if (!actionsJsonBlock.TryGetProperty("OnSuccess", out var onSuccessBlock) ||
                    !onSuccessBlock.TryGetProperty("Context", out var effectContextBlock))
                    return null;

                var context = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(effectContextBlock.GetRawText())
                              ?? new Dictionary<string, JsonElement>();

                var effectType = ReadStringFromContext(context, "effectType");
                var effectValue = decimal.TryParse(ReadStringFromContext(context, "value"), out var ev) ? ev : 0m;
                var expression = ReadStringFromContext(context, "Expression");
                var parameterCode = ReadStringFromContext(context, "ParameterCode");

                var baseRate = ResolveBaseRate(ruleName, parameterCode, inputValues);

                var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(effectType));
                decimal computedRate;

                if (applicator != null)
                {
                    // RateLookupApplicator needs the full context and input dictionaries
                    if (applicator is RateLookupApplicator rateLookupApplicator)
                    {
                        rateLookupApplicator.SetLookupContext(
                            context.ToDictionary(k => k.Key, k => JsonElementToObject(k.Value)));
                        rateLookupApplicator.SetInputDictionary(inputValues);
                    }
                    computedRate = await applicator.Apply(baseRate, effectValue);
                }
                else
                {
                    _logger.LogWarning(
                        "No IRuleEffectApplicator registered for effectType='{EffectType}'. Rate unchanged.",
                        effectType);
                    computedRate = baseRate;
                }

                return new RuleExecutionResultDto
                {
                    RuleCode = ruleName,
                    RuleName = !string.IsNullOrWhiteSpace(ruleErrorMessage) ? ruleErrorMessage : ruleName,
                    EffectType = effectType,
                    EffectValue = effectValue,
                    BaseRate = baseRate,
                    ComputedRate = Math.Round(computedRate, 4, MidpointRounding.AwayFromZero),
                    Expression = expression,
                    Context = context.ToDictionary(k => k.Key, k => k.Value.ToString())
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build result for rule '{RuleName}'.", ruleName);
                return null;
            }
        }

        /// <summary>
        /// Resolves the base rate value from the input dictionary using the
        /// <c>ParameterCode</c> field from the rule's effect context.
        /// Falls back to common rate field names if <c>ParameterCode</c> is absent or unresolved.
        /// </summary>
        private decimal ResolveBaseRate(
            string ruleName,
            string parameterCode,
            Dictionary<string, object> inputValues)
        {
            // Try the explicit ParameterCode first (e.g. "input.Rate" → key "Rate")
            var rateKey = parameterCode.Replace("input.", "").Trim();
            if (!string.IsNullOrEmpty(rateKey) && inputValues.TryGetValue(rateKey, out var rateObj))
                return Convert.ToDecimal(UnwrapJsonValue(rateObj));

            // Fall back to well-known rate field names
            foreach (var fallbackKey in new[] { "Rate", "RatePerSqMt", "BaseRate" })
            {
                if (inputValues.TryGetValue(fallbackKey, out var fallbackValue))
                    return Convert.ToDecimal(UnwrapJsonValue(fallbackValue));
            }

            _logger.LogWarning(
                "Could not resolve base rate for rule '{RuleName}'. " +
                "ParameterCode='{ParameterCode}'. Available keys: {Keys}",
                ruleName, parameterCode, string.Join(", ", inputValues.Keys));

            return 0m;
        }

        // ─── Expression Utilities ───────────────────────────────────────────────────

        /// <summary>
        /// Replaces SQL-style logical operators (<c>AND</c>, <c>OR</c>, <c>NOT</c>) with
        /// their C# equivalents (<c>&amp;&amp;</c>, <c>||</c>, <c>!</c>) so that
        /// <c>System.Linq.Dynamic.Core</c> can evaluate the expression correctly.
        /// Word-boundary matching prevents modifying field names that contain these words.
        /// </summary>
        private static string NormalizeLogicalOperators(string expression)
        {
            expression = Regex.Replace(expression, @"\bAND\b", "&&", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"\bOR\b", "||", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"\bNOT\b", "!", RegexOptions.IgnoreCase);
            expression = Regex.Replace(expression, @"(?<!\.)\bcontains\b", "in", RegexOptions.IgnoreCase);
            return expression;
        }

        /// <summary>
        /// Guards against code injection by validating the expression length, allowed patterns,
        /// and the absence of dangerous .NET API keywords before the rule is registered
        /// with the MS Rules Engine.
        /// </summary>
        private static bool IsExpressionSafe(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            // Reject excessively long expressions (DoS guard)
            if (expression.Length > 1000)
                return false;

            // Reject expressions containing dangerous .NET namespaces / types
            var blockedKeywords = new[]
            {
                "System.", "Reflection", "Assembly", "Type.", "Activator",
                "Process", "File", "Directory", "Registry", "Environment",
                "AppDomain", "Thread", "Task", "Delegate", "Invoke"
            };
            foreach (var keyword in blockedKeywords)
            {
                if (expression.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Reject expressions with more than 10 levels of parenthesis nesting (stack-overflow guard)
            int depth = 0, maxDepth = 0;
            foreach (char c in expression)
            {
                if (c == '(') maxDepth = Math.Max(maxDepth, ++depth);
                else if (c == ')') depth--;
            }
            if (maxDepth > 10)
                return false;

            // Strip all known-safe patterns; any residual characters are considered unsafe
            var remaining = expression;
            foreach (var safePattern in new[]
            {
                @"\binput\.\w+\b",          // input.PropertyName
                @"\.\w+",                   // Property access / method calls (e.g. .Contains)
                @"\d+(\.\d+)?",             // Numbers
                @"[<>=!&|]+",               // Comparison / logical operators
                @"\bAND\b|\bOR\b|\bNOT\b|\bin\b|\bcontains\b", // SQL-style & SQL collection logical operators
                @"[(){},]",                 // Parentheses / braces / commas
                @"\s+",                     // Whitespace
                @"true|false",              // Boolean literals
                @"'[^']*'|""[^""]*"""       // String literals (single or double quoted)
            })
            {
                remaining = Regex.Replace(remaining, safePattern, "", RegexOptions.IgnoreCase);
            }

            return string.IsNullOrWhiteSpace(remaining.Trim());
        }

        // ─── Dynamic Input Builders ─────────────────────────────────────────────────

        /// <summary>
        /// Converts a <c>Dictionary&lt;string, object&gt;</c> into a dynamic
        /// <see cref="ExpandoObject"/> so MS Rules Engine lambda expressions
        /// (e.g. <c>input.Floor == 65</c>) can resolve property access at runtime.
        /// Handles nested dictionaries and <see cref="JsonElement"/> values.
        /// </summary>
        private static ExpandoObject BuildDynamicInput(Dictionary<string, object> inputDict)
        {
            if (inputDict == null || !inputDict.Any())
                throw new ArgumentException("Input dictionary cannot be null or empty.", nameof(inputDict));

            var expando = (IDictionary<string, object>)new ExpandoObject();
            var validCount = 0;

            foreach (var kvp in inputDict)
            {
                if (kvp.Value == null) continue;

                try
                {
                    expando[kvp.Key] = UnwrapJsonValue(kvp.Value);
                    validCount++;
                }
                catch
                {
                    // Skip individual values that cannot be unwrapped; do not abort the whole input
                }
            }

            if (validCount == 0)
                throw new ArgumentException("Input dictionary contains no valid values.", nameof(inputDict));

            return (ExpandoObject)expando;
        }

        /// <summary>
        /// Unwraps a single value from the input dictionary into a CLR-native type
        /// that the dynamic expression evaluator can work with.
        /// </summary>
        private static object UnwrapJsonValue(object value)
        {
            if (value is JsonElement jsonEl)
            {
                return jsonEl.ValueKind switch
                {
                    JsonValueKind.Number when jsonEl.TryGetInt32(out int i) => i,
                    JsonValueKind.Number when jsonEl.TryGetDouble(out double d) => d,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => jsonEl.GetString() ?? string.Empty,
                    JsonValueKind.Object => JsonObjectToExpando(jsonEl),
                    JsonValueKind.Array => JsonArrayToList(jsonEl),
                    _ => jsonEl.ToString()
                };
            }

            if (value is Dictionary<string, object> nested)
                return BuildDynamicInput(nested);

            return value;
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> of kind <c>Array</c>
        /// into a <see cref="List{Object}"/> of unwrapped values.
        /// </summary>
        private static List<object> JsonArrayToList(JsonElement array)
        {
            var list = new List<object>();
            foreach (var item in array.EnumerateArray())
            {
                list.Add(UnwrapJsonValue(item));
            }
            return list;
        }

        /// <summary>
        /// Recursively converts a <see cref="JsonElement"/> of kind <c>Object</c>
        /// into an <see cref="ExpandoObject"/> for nested property access.
        /// </summary>
        private static ExpandoObject JsonObjectToExpando(JsonElement obj)
        {
            var expando = (IDictionary<string, object>)new ExpandoObject();
            foreach (var prop in obj.EnumerateObject())
                expando[prop.Name] = UnwrapJsonValue(prop.Value);
            return (ExpandoObject)expando;
        }

        // ─── Context Extraction Utilities ───────────────────────────────────────────

        /// <summary>
        /// Reads a string value from a <c>Context</c> dictionary by key.
        /// Returns <see cref="string.Empty"/> if the key is absent or the value is null.
        /// </summary>
        private static string ReadStringFromContext(Dictionary<string, JsonElement> context, string key)
        {
            if (context.TryGetValue(key, out var el))
                return el.ValueKind == JsonValueKind.String
                    ? el.GetString() ?? string.Empty
                    : el.ToString();
            return string.Empty;
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> to its native CLR equivalent.
        /// Used when injecting context values into <see cref="RateLookupApplicator"/>.
        /// </summary>
        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out int i) => i,
                JsonValueKind.Number when element.TryGetInt64(out long l) => l,
                JsonValueKind.Number when element.TryGetDecimal(out decimal d) => d,
                JsonValueKind.Number when element.TryGetDouble(out double dbl) => dbl,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Null => null!,
                _ => element.ToString()
            };
        }
    }
}
