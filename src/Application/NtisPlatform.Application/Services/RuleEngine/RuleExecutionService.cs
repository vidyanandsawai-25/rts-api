using System.Dynamic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Interfaces.RuleEngine;
using NtisPlatform.Application.Services.RuleEngine.Effects;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using RulesEngine.Models;

namespace NtisPlatform.Application.Services.RuleEngine
{
    /// <summary>
    /// Executes Microsoft Rules Engine policies stored in RuleEngineMaster.RuleJson
    /// against a dynamic property tax input and returns matched rule effects.
    /// </summary>
    public class RuleExecutionService : IRuleExecutionService
    {
        private readonly IRepository<RuleEngineEntity, int> _ruleRepository;
        private readonly IRepository<RuleCategoryEntity, int> _categoryRepository;
        private readonly IRepository<RuleExclusionEntity, int> _ruleExclusionRepository;
        private readonly IEnumerable<IRuleEffectApplicator> _effectApplicators;
        private readonly ILogger<RuleExecutionService> _logger;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "RuleEngine_";
        private const string ExclusionCacheKeyPrefix = "RuleExclusions_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        public RuleExecutionService(
            IRepository<RuleEngineEntity, int> ruleRepository,
            IRepository<RuleCategoryEntity, int> categoryRepository,
            IRepository<RuleExclusionEntity, int> ruleExclusionRepository,
            IEnumerable<IRuleEffectApplicator> effectApplicators,
            ILogger<RuleExecutionService> logger,
            IMemoryCache cache)
        {
            _ruleRepository = ruleRepository;
            _categoryRepository = categoryRepository;
            _ruleExclusionRepository = ruleExclusionRepository;
            _effectApplicators = effectApplicators;
            _logger = logger;
            _cache = cache;
        }

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
            // Validate input
            if (input == null)
                throw new ArgumentNullException(nameof(input), "RuleExecutionInputDto cannot be null");

            if (string.IsNullOrWhiteSpace(input.Category))
                throw new ArgumentException("Category is required.", nameof(input));

            if (input.Input == null || !input.Input.Any())
                throw new ArgumentException("Input dictionary cannot be null or empty.", nameof(input));

            // P3: Track execution start
            var executionStopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Executing rules for category={Category}", input.Category);

            // Check cache first for performance optimization (thread-safe IMemoryCache)
            var cacheKey = $"{CacheKeyPrefix}{input.Category}";
            var cacheHit = _cache.TryGetValue(cacheKey, out var _);

            var cached = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    // Set cache options with size limit and sliding expiration
                    entry.SetSlidingExpiration(CacheExpiration);
                    entry.SetSize(1); // Each category = 1 size unit
                    entry.SetPriority(CacheItemPriority.High);

                    // P3: Log cache miss metric
                    _logger.LogInformation("[Metric] RuleEngine.CacheMiss = 1 for category={Category}", input.Category);
                    _logger.LogDebug("Cache miss for category={Category}, loading from database", input.Category);

                    // Cache miss - load rules from database ordered by Priority (lower value = higher priority)
                    // ThenBy(Id) ensures deterministic ordering when multiple rules have same priority
                    var ruleEntities = await _ruleRepository.GetQueryable()
                        .Where(r => r.RuleCategory == input.Category && r.IsEnabled && r.IsActive)
                        .OrderBy(r => r.Priority)
                        .ThenBy(r => r.Id)  // ✅ FIX: Tie-breaker for deterministic execution order
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    if (!ruleEntities.Any())
                    {
                        _logger.LogDebug("No enabled rules found for category={Category}", input.Category);
                        return (
                            Workflows: new List<(Workflow, Dictionary<string, JsonElement>, int, bool)>(),
                            Engine: (global::RulesEngine.RulesEngine)null!
                        );
                    }

                    _logger.LogDebug("Loaded {Count} rules for category={Category}", ruleEntities.Count, input.Category);
                    var workflows = BuildWorkflowsWithMetadata(ruleEntities);
                    if (workflows.Any())
                    {
                        var engine = new global::RulesEngine.RulesEngine(workflows.Select(x => x.Workflow).ToArray());
                        return (Workflows: workflows, Engine: engine);
                    }

                    return (
                        Workflows: new List<(Workflow, Dictionary<string, JsonElement>, int, bool)>(),
                        Engine: (global::RulesEngine.RulesEngine)null!
                    );
                });

            // P3: Log cache hit metric
            if (cacheHit)
            {
                _logger.LogInformation("[Metric] RuleEngine.CacheHit = 1 for category={Category}", input.Category);
            }

            // Load rule exclusions (cached separately for all categories)
            var exclusions = await LoadRuleExclusionsAsync(cancellationToken);

            var workflowsWithMetadata = cached.Workflows;
            var engineInstance = cached.Engine;

            if (!workflowsWithMetadata.Any() || engineInstance == null)
                return new List<RuleExecutionResultDto>();

            // Build a typed RuleParameter named "input" so expressions like
            // "input.Floor == 65" resolve correctly in MS Rules Engine
            var expandoInput = BuildExpandoInput(input.Input);
            var ruleParam = new RuleParameter("input", expandoInput);

            // 🔍 DEBUG: Log the input parameters being sent to rule engine
            _logger.LogInformation(
                "🔍 [RuleEngine-DEBUG] Executing rules with input parameters: {@InputParams}",
                input.Input);

            // Initialize tracking sets for exclusion logic
            var skippedRuleIds = new HashSet<int>();
            var appliedRuleIds = new HashSet<int>();
            var results = new List<RuleExecutionResultDto>();

            // Execute all workflows with exclusion and stop processing logic
            foreach (var (workflow, actionsMap, ruleId, stopProcessing) in workflowsWithMetadata)
            {
                try
                {
                    // ⏹️ Check if this rule should be skipped due to exclusions
                    if (skippedRuleIds.Contains(ruleId))
                    {
                        _logger.LogInformation(
                            "⏹️ [RuleEngine-Exclusion] Skipping rule '{RuleCode}' (Id={RuleId}) due to exclusion",
                            workflow.WorkflowName, ruleId);
                        continue;
                    }

                    // 🔍 DEBUG: Log which workflow is being executed
                    _logger.LogInformation(
                        "🔍 [RuleEngine-DEBUG] Executing workflow: {WorkflowName}, Rules count: {RulesCount}",
                        workflow.WorkflowName, workflow.Rules?.Count() ?? 0);

                    // Log each rule expression for debugging
                    if (workflow.Rules != null)
                    {
                        foreach (var rule in workflow.Rules)
                        {
                            _logger.LogInformation(
                                "🔍 [RuleEngine-DEBUG] Rule: {RuleName}, Expression: {Expression}",
                                rule.RuleName, rule.Expression);
                        }
                    }

                    // Pass as named RuleParameter so "input.X" expressions resolve
                    var ruleResults = await engineInstance.ExecuteAllRulesAsync(
                        workflow.WorkflowName,
                        new[] { ruleParam });

                    foreach (var ruleResult in ruleResults)
                    {
                        if (ruleResult.IsSuccess)
                        {
                            _logger.LogInformation(
                                "🔍 [RuleEngine-DEBUG] ✅ Rule MATCHED: workflow={WorkflowName}, rule={RuleName}, expression={Expression}",
                                workflow.WorkflowName, ruleResult.Rule.RuleName, ruleResult.Rule.Expression);

                            // Look up the raw actions context from our side dictionary
                            var rawActions = actionsMap.TryGetValue(ruleResult.Rule.RuleName, out var a)
                                ? (JsonElement?)a
                                : null;
                            var result = await BuildResult(ruleResult.Rule.RuleName, rawActions, input.Input);
                            if (result != null)
                            {
                                _logger.LogInformation(
                                    "🔍 [RuleEngine-DEBUG] Rule effect: {EffectType} {EffectValue}%, BaseRate={BaseRate}, ComputedRate={ComputedRate}",
                                    result.EffectType, result.EffectValue, result.BaseRate, result.ComputedRate);

                                // 🔹 Mark this rule as applied
                                result.StopProcessing = stopProcessing;
                                results.Add(result);
                                appliedRuleIds.Add(ruleId);

                                // 🔹 Apply exclusions: mark other rules to be skipped
                                var rulesToSkip = exclusions
                                    .Where(x => x.AppliedRuleId == ruleId)
                                    .Select(x => x.SkipRuleId)
                                    .ToList();

                                if (rulesToSkip.Any())
                                {
                                    _logger.LogInformation(
                                        "⏹️ [RuleEngine-Exclusion] Rule '{RuleCode}' (Id={RuleId}) triggered exclusions. " +
                                        "Marking {Count} rule(s) to be skipped: {SkipRuleIds}",
                                        workflow.WorkflowName, ruleId, rulesToSkip.Count, string.Join(", ", rulesToSkip));

                                    foreach (var skipRuleId in rulesToSkip)
                                    {
                                        skippedRuleIds.Add(skipRuleId);
                                    }
                                }

                                // 🔹 Check StopProcessing flag
                                if (stopProcessing)
                                {
                                    _logger.LogInformation(
                                        "🛑 [RuleEngine-StopProcessing] Rule '{RuleCode}' (Id={RuleId}) has StopProcessing=true. " +
                                        "Halting all remaining rule execution.",
                                        workflow.WorkflowName, ruleId);

                                    // Exit all loops immediately
                                    executionStopwatch.Stop();
                                    _logger.LogInformation(
                                        "[RuleEngine] Execution stopped early for category={Category}. {MatchCount} rules matched before stop.",
                                        input.Category, results.Count);
                                    return results;
                                }
                            }
                        }
                        else
                        {
                            var failMsg = ruleResult.ExceptionMessage ?? ruleResult.Rule.ErrorMessage ?? "condition not met";
                            _logger.LogInformation(
                                "🔍 [RuleEngine-DEBUG] ❌ Rule NOT MATCHED: workflow={WorkflowName}, rule={RuleName}, expression={Expression}, reason={Reason}",
                                workflow.WorkflowName, ruleResult.Rule.RuleName, ruleResult.Rule.Expression, failMsg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[RuleEngine] Exception executing workflow={WorkflowName}. Input={@Input}",
                        workflow.WorkflowName, input.Input);
                }
            }

            _logger.LogInformation(
                "[RuleEngine] Execution complete for category={Category}. {MatchCount}/{TotalRules} rules matched, {SkippedCount} skipped.",
                input.Category, results.Count, workflowsWithMetadata.Count, skippedRuleIds.Count);

            executionStopwatch.Stop();

            // P3: Log execution metrics
            _logger.LogInformation(
                "[Metric] RuleEngine.ExecutionDuration = {DurationMs} ms for category={Category}",
                executionStopwatch.ElapsedMilliseconds, input.Category);

            _logger.LogInformation(
                "[Metric] RuleEngine.RulesMatched = {MatchedCount} for category={Category}",
                results.Count, input.Category);

            _logger.LogInformation(
                "[Metric] RuleEngine.RulesSkipped = {SkippedCount} for category={Category}",
                skippedRuleIds.Count, input.Category);

            return results;
        }

        /// <summary>
        /// Loads rule exclusions from database with caching.
        /// Returns dictionary mapping AppliedRuleId → list of SkipRuleIds.
        /// </summary>
        private async Task<List<RuleExclusionEntity>> LoadRuleExclusionsAsync(CancellationToken cancellationToken = default)
        {
            var exclusionCacheKey = ExclusionCacheKeyPrefix + "All";

            return await _cache.GetOrCreateAsync(
                exclusionCacheKey,
                async entry =>
                {
                    entry.SetSlidingExpiration(CacheExpiration);
                    entry.SetSize(1);
                    entry.SetPriority(CacheItemPriority.High);

                    _logger.LogDebug("Loading rule exclusions from database");

                    var exclusions = await _ruleExclusionRepository.GetQueryable()
                        .Where(x => x.IsActive)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation("Loaded {Count} active rule exclusions", exclusions.Count);
                    return exclusions;
                }) ?? new List<RuleExclusionEntity>();
        }

        // ─── Private Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Maps stored RuleJson to MS Rules Engine Workflow objects.
        /// Returns a tuple of (Workflow, ActionsMap, RuleId, StopProcessing) where:
        /// - Workflow: MS Rules Engine workflow object
        /// - ActionsMap: raw Actions JSON per ruleName
        /// - RuleId: the rule entity ID for exclusion tracking
        /// - StopProcessing: flag to halt all remaining rules
        /// </summary>
        private List<(Workflow Workflow, Dictionary<string, JsonElement> ActionsMap, int RuleId, bool StopProcessing)> BuildWorkflowsWithMetadata(
            List<RuleEngineEntity> entities)
        {
            var workflows = new List<(Workflow, Dictionary<string, JsonElement>, int, bool)>();

            foreach (var entity in entities)
            {
                if (string.IsNullOrWhiteSpace(entity.RuleJson))
                    continue;

                try
                {
                    var policy = JsonSerializer.Deserialize<JsonElement>(entity.RuleJson);

                    // Extract outer policy name → WorkflowName
                    var workflowName = entity.RuleCode; // Use RuleCode as unique workflow name
                    if (policy.TryGetProperty("RuleName", out var policyNameEl))
                        workflowName = policyNameEl.GetString() ?? workflowName;

                    // Extract rules array
                    if (!policy.TryGetProperty("rules", out var rulesEl) ||
                        rulesEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var rules = new List<Rule>();
                    var actionsMap = new Dictionary<string, JsonElement>(); // ruleName → raw Actions JsonElement
                    foreach (var ruleEl in rulesEl.EnumerateArray())
                    {
                        // RuleName for MS Rules Engine: prefer RuleCode, fallback to ruleName (lowercase), then entity.RuleCode
                        var ruleName = entity.RuleCode;
                        if (ruleEl.TryGetProperty("RuleCode", out var rcEl) &&
                            !string.IsNullOrWhiteSpace(rcEl.GetString()))
                            ruleName = rcEl.GetString()!;
                        else if (ruleEl.TryGetProperty("ruleName", out var rnEl) &&
                                 !string.IsNullOrWhiteSpace(rnEl.GetString()))
                            ruleName = rnEl.GetString()!;

                        // Expression
                        if (!ruleEl.TryGetProperty("expression", out var exprEl))
                            continue;
                        var expression = exprEl.GetString();
                        if (string.IsNullOrWhiteSpace(expression))
                            continue;

                        // enabled flag
                        var enabled = true;
                        if (ruleEl.TryGetProperty("enabled", out var enabledEl))
                            enabled = enabledEl.GetBoolean();
                        if (!enabled) continue;

                        // P1: Security validation - prevent expression injection attacks
                        if (!ValidateExpressionSecurity(expression))
                        {
                            _logger.LogWarning(
                                "[RuleEngine] Security validation failed for rule={RuleName}. Expression contains unsafe code: {Expression}",
                                ruleName, expression);
                            continue; // Skip unsafe rule
                        }

                        // Build Rule — Actions stored as raw JSON for OnSuccess context extraction
                        var normalizedExpr = NormalizeExpression(expression);
                        _logger.LogDebug(
                            "[RuleEngine] Building rule: name={RuleName}, expr={Expression}",
                            ruleName, normalizedExpr);

                        var rule = new Rule
                        {
                            RuleName = ruleName,
                            // Normalize SQL-style AND/OR to C# && || for Dynamic LINQ compatibility
                            Expression = normalizedExpr,
                            RuleExpressionType = RuleExpressionType.LambdaExpression,
                            // DO NOT set rule.Actions — MS Rules Engine would try to invoke
                            // "Decrease" as a registered IAction plugin (which doesn't exist).
                            // We handle effect application ourselves via IRuleEffectApplicator.
                        };

                        rules.Add(rule);

                        // Store actions JSON in our own side dictionary keyed by ruleName
                        // First try to get Actions from RuleJson
                        if (ruleEl.TryGetProperty("Actions", out var actionsEl) &&
                            actionsEl.ValueKind != JsonValueKind.Null)
                        {
                            actionsMap[ruleName] = actionsEl;
                        }
                        // If Actions is null/missing in RuleJson, use EffectJson from entity
                        else if (!string.IsNullOrWhiteSpace(entity.EffectJson))
                        {
                            try
                            {
                                var effectJson = JsonSerializer.Deserialize<JsonElement>(entity.EffectJson);

                                // Wrap EffectJson in the expected Actions structure:
                                // { "OnSuccess": { "Context": { effectType: "...", value: ... } } }
                                var wrappedActions = new
                                {
                                    OnSuccess = new
                                    {
                                        Context = effectJson
                                    }
                                };

                                var wrappedJson = JsonSerializer.Serialize(wrappedActions);
                                actionsMap[ruleName] = JsonSerializer.Deserialize<JsonElement>(wrappedJson);

                                _logger.LogInformation(
                                    "🔍 [RuleEngine-DEBUG] Using EffectJson for rule={RuleName} (Actions was null in RuleJson). Effect: {EffectJson}",
                                    ruleName, entity.EffectJson);
                            }
                            catch (Exception effEx)
                            {
                                _logger.LogWarning(effEx,
                                    "[RuleEngine] Failed to parse EffectJson for rule={RuleName}",
                                    ruleName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine] No Actions or EffectJson found for rule={RuleName}",
                                ruleName);
                        }

                    }

                    if (!rules.Any())
                        continue;

                    // Include RuleId and StopProcessing flag in the tuple
                    workflows.Add((
                        new Workflow { WorkflowName = workflowName, Rules = rules },
                        actionsMap,
                        entity.Id,
                        entity.StopProcessing
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse RuleJson for RuleCode={RuleCode}. Skipping.", entity.RuleCode);
                }
            }

            return workflows;
        }

        /// <summary>
        /// Normalizes SQL-style operators (AND, OR, NOT) to C# equivalents (&amp;&amp;, ||, !)
        /// so System.Linq.Dynamic.Core evaluates them correctly.
        /// e.g. "input.Floor == 65 AND input.TypeOfUseGroup == 1" becomes "input.Floor == 65 &amp;&amp; input.TypeOfUseGroup == 1"
        /// </summary>
        private static string NormalizeExpression(string expression)
        {
            // Use word-boundary replacement to avoid modifying field names that contain AND/OR
            return System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(
                    System.Text.RegularExpressions.Regex.Replace(
                        expression,
                        @"\bAND\b", "&&",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    @"\bOR\b", "||",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                @"\bNOT\b", "!",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, object&gt; into a dynamic ExpandoObject
        /// so MS Rules Engine can access properties via lambda (e.g. input.Floor).
        /// Handles nested dictionaries and JsonElement values (flat + nested input).
        /// </summary>
        private static ExpandoObject BuildExpandoInput(Dictionary<string, object> inputDict)
        {
            if (inputDict == null || !inputDict.Any())
                throw new ArgumentException("Input dictionary cannot be null or empty", nameof(inputDict));

            var expando = new ExpandoObject() as IDictionary<string, object>;
            var validKeyCount = 0;

            foreach (var kvp in inputDict)
            {
                // Skip null values but log warning
                if (kvp.Value == null)
                {
                    // Note: Cannot use _logger here as this is a static method
                    // Null values are silently skipped
                    continue;
                }

                try
                {
                    expando[kvp.Key] = UnwrapValue(kvp.Value);
                    validKeyCount++;
                }
                catch (Exception)
                {
                    // Skip problematic values but continue processing others
                    continue;
                }
            }

            if (validKeyCount == 0)
                throw new ArgumentException("Input dictionary contains no valid values", nameof(inputDict));

            return expando as ExpandoObject ?? new ExpandoObject();
        }

        private static object UnwrapValue(object value)
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
                    JsonValueKind.Object => BuildExpandoFromJsonElement(jsonEl),
                    _ => jsonEl.ToString()
                };
            }

            if (value is Dictionary<string, object> nested)
                return BuildExpandoInput(nested);

            return value;
        }

        private static ExpandoObject BuildExpandoFromJsonElement(JsonElement obj)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;
            foreach (var prop in obj.EnumerateObject())
                expando[prop.Name] = UnwrapValue(prop.Value);
            return expando as ExpandoObject ?? new ExpandoObject();
        }

        /// <summary>
        /// Extracts the OnSuccess Context from our side actionsMap and builds the
        /// RuleExecutionResultDto, computing the adjusted rate via IRuleEffectApplicator.
        /// </summary>
        private async Task<RuleExecutionResultDto?> BuildResult(
            string ruleName,
            JsonElement? rawActionsEl,
            Dictionary<string, object> inputDict)
        {
            try
            {
                if (rawActionsEl == null)
                    return null;

                var actionsEl = rawActionsEl.Value;

                // Navigate: Actions.OnSuccess.Context
                if (!actionsEl.TryGetProperty("OnSuccess", out var onSuccessEl))
                    return null;
                if (!onSuccessEl.TryGetProperty("Context", out var contextEl))
                    return null;

                var context = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(contextEl.GetRawText())
                              ?? new Dictionary<string, JsonElement>();

                var effectType = GetContextString(context, "effectType");
                var valueStr = GetContextString(context, "value");
                var expression = GetContextString(context, "Expression");
                var parameterCode = GetContextString(context, "ParameterCode");

                if (!decimal.TryParse(valueStr, out decimal effectValue))
                    effectValue = 0m;

                var rateKey = parameterCode.Replace("input.", "").Trim();
                decimal baseRate = 0m;

                if (!string.IsNullOrEmpty(rateKey) && inputDict.TryGetValue(rateKey, out var rateObj))
                {
                    baseRate = Convert.ToDecimal(UnwrapValue(rateObj));
                }
                else
                {
                    // ParameterCode not set or not found — try common rate keys as fallback
                    var fallbackKeys = new[] { "Rate", "RatePerSqMt", "BaseRate" };
                    foreach (var fk in fallbackKeys)
                    {
                        if (inputDict.TryGetValue(fk, out var fv))
                        {
                            baseRate = Convert.ToDecimal(UnwrapValue(fv));
                            _logger.LogDebug(
                                "[RuleEngine] ParameterCode missing/unresolved for rule={RuleName}. Fell back to '{FallbackKey}'={BaseRate}",
                                ruleName, fk, baseRate);
                            break;
                        }
                    }
                    if (baseRate == 0m)
                        _logger.LogWarning(
                            "[RuleEngine] Could not resolve base rate for rule={RuleName}. ParameterCode='{ParameterCode}'. Available keys: {Keys}",
                            ruleName, parameterCode, string.Join(", ", inputDict.Keys));
                }

                var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(effectType));
                decimal computedRate;

                if (applicator != null)
                {
                    // If applicator supports context injection (e.g., RateLookupApplicator), provide it
                    if (applicator is Effects.RateLookupApplicator rateLookupApplicator)
                    {
                        // Convert JsonElement context values to object dictionary
                        var contextDict = context.ToDictionary(
                            k => k.Key,
                            k => ConvertJsonElementToObject(k.Value)
                        );
                        rateLookupApplicator.SetLookupContext(contextDict);

                        // Pass input dictionary for extracting TaxZoneId, ConstructionTypeId, etc.
                        rateLookupApplicator.SetInputDictionary(inputDict);
                    }

                    computedRate = await applicator.Apply(baseRate, effectValue);
                }
                else
                {
                    _logger.LogWarning("No IRuleEffectApplicator found for effectType='{EffectType}'. Rate unchanged.", effectType);
                    computedRate = baseRate;
                }

                var contextStrings = context.ToDictionary(k => k.Key, k => k.Value.ToString());

                return new RuleExecutionResultDto
                {
                    RuleCode = ruleName,
                    RuleName = ruleName,
                    EffectType = effectType,
                    EffectValue = effectValue,
                    BaseRate = baseRate,
                    ComputedRate = Math.Round(computedRate, 4, MidpointRounding.AwayFromZero),
                    Expression = expression,
                    Context = contextStrings
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build result for rule={RuleName}.", ruleName);
                return null;
            }
        }

        private static string GetContextString(Dictionary<string, JsonElement> context, string key)
        {
            if (context.TryGetValue(key, out var el))
                return el.ValueKind == JsonValueKind.String ? el.GetString() ?? string.Empty : el.ToString();
            return string.Empty;
        }

        /// <summary>
        /// Converts a JsonElement to its underlying object type for context passing.
        /// </summary>
        private static object ConvertJsonElementToObject(JsonElement element)
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

        /// <summary>
        /// P0: Cache invalidation - clears cached rules for a category or all categories.
        /// Call this when rules are updated/created/deleted in the database.
        /// </summary>
        public void InvalidateCache(string? category = null)
        {
            if (category == null)
            {
                // Clear all cached rules (triggered on bulk updates or admin request)
                // IMemoryCache interface doesn't expose Compact, but MemoryCache implementation does
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0); // Remove 100% of cache entries
                    _logger.LogInformation("[RuleEngine] Cleared entire rule cache (compacted 100%)");
                }
                else
                {
                    _logger.LogWarning("[RuleEngine] Cannot compact cache - IMemoryCache implementation doesn't support Compact()");
                }
            }
            else
            {
                var cacheKey = $"{CacheKeyPrefix}{category}";
                _cache.Remove(cacheKey);
                _logger.LogInformation("[RuleEngine] Invalidated cache for category={Category}", category);
            }
        }

        /// <summary>
        /// P1: Expression security validation - prevents code injection attacks.
        /// Validates that expressions only use safe operators and don't exceed complexity limits.
        /// </summary>
        private static bool ValidateExpressionSecurity(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            // Maximum expression length to prevent DoS
            if (expression.Length > 1000)
                return false;

            // Whitelist allowed operators and keywords
            var allowedPatterns = new[]
            {
                @"\binput\.\w+\b",           // input.PropertyName
                @"\d+(\.\d+)?",               // Numbers
                @"[<>=!&|]+",                 // Comparison operators
                @"\bAND\b|\bOR\b|\bNOT\b",   // Logical operators (will be normalized)
                @"[(){}]",                    // Parentheses
                @"\s+",                       // Whitespace
                @"true|false",                // Booleans
                @"'[^']*'|""[^""]*"""         // String literals
            };

            // Remove all allowed patterns
            var remaining = expression;
            foreach (var pattern in allowedPatterns)
            {
                remaining = System.Text.RegularExpressions.Regex.Replace(
                    remaining, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // If anything remains, it contains potentially dangerous code
            remaining = remaining.Trim();
            if (!string.IsNullOrEmpty(remaining))
            {
                // Contains disallowed characters/keywords
                return false;
            }

            // Check for dangerous keywords that could execute code
            var dangerousKeywords = new[]
            {
                "System.", "Reflection", "Assembly", "Type.", "Activator",
                "Process", "File", "Directory", "Registry", "Environment",
                "AppDomain", "Thread", "Task", "Delegate", "Invoke"
            };

            foreach (var keyword in dangerousKeywords)
            {
                if (expression.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Limit nesting depth (prevent stack overflow)
            int depth = 0, maxDepth = 0;
            foreach (char c in expression)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                maxDepth = Math.Max(maxDepth, depth);
            }

            return maxDepth <= 10; // Max 10 levels of nesting
        }
    }
}
