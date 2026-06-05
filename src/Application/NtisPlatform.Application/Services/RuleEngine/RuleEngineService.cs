using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Interfaces.RuleEngine;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RuleEngine
{
    /// <summary>
    /// Service for managing rule engine configurations with automatic versioning
    /// </summary>
    public class RuleEngineService : BaseCommonCrudService<RuleEngineEntity, RuleEngineDto, CreateRuleEngineDto, UpdateRuleEngineDto, RuleEngineQueryParameters, int>, IRuleEngineService
    {
        private readonly IRepository<RuleVersionHistoryEntity, long> _versionHistoryRepository;
        private readonly IRepository<RuleExclusionEntity, int> _ruleExclusionRepository;
        private readonly IRuleExecutionService _ruleExecutionService;

        public RuleEngineService(
            IRepository<RuleEngineEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRepository<RuleVersionHistoryEntity, long> versionHistoryRepository,
            IRepository<RuleExclusionEntity, int> ruleExclusionRepository,
            IRuleExecutionService ruleExecutionService)
            : base(repository, unitOfWork, mapper)
        {
            _versionHistoryRepository = versionHistoryRepository;
            _ruleExclusionRepository = ruleExclusionRepository;
            _ruleExecutionService = ruleExecutionService;
        }

        /// <summary>
        /// Override to include RuleScope navigation property
        /// </summary>
        protected override IQueryable<RuleEngineEntity> ApplyIncludes(IQueryable<RuleEngineEntity> query)
        {
            return query;
        }

        /// <summary>
        /// Override GetByIdAsync to include rule exclusions
        /// </summary>
        public override async Task<RuleEngineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            var dto = _mapper.Map<RuleEngineDto>(entity);

            // Load rule exclusions (rules that will be skipped when this rule applies)
            await PopulateRuleExclusionsAsync(dto, cancellationToken);

            return dto;
        }

        /// <summary>
        /// Override GetAllAsync to include rule exclusions for all rules
        /// </summary>
        public override async Task<Models.PagedResult<RuleEngineDto>> GetAllAsync(
            RuleEngineQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            var pagedResult = await base.GetAllAsync(queryParameters, cancellationToken);

            // Optimize: Load exclusions for all rules in a single query to avoid N+1 problem
            var ruleIds = pagedResult.Items.Select(r => r.Id).ToList();
            if (ruleIds.Any())
            {
                var allExclusions = _ruleExclusionRepository.GetQueryable()
                    .Where(e => ruleIds.Contains(e.AppliedRuleId) && e.IsActive)
                    .Include(e => e.SkipRule)
                    .ToList()
                    .GroupBy(e => e.AppliedRuleId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var dto in pagedResult.Items)
                {
                    if (allExclusions.TryGetValue(dto.Id, out var exclusions))
                    {
                        dto.SkipRuleIds = exclusions.Select(e => e.SkipRuleId).ToList();
                        dto.SkipRules = exclusions.Select(e => new SkipRuleInfo
                        {
                            RuleId = e.SkipRuleId,
                            RuleCode = e.SkipRule.RuleCode,
                            RuleName = e.SkipRule.RuleName,
                            Reason = e.Reason
                        }).ToList();
                    }
                    else
                    {
                        dto.SkipRuleIds = new List<int>();
                        dto.SkipRules = new List<SkipRuleInfo>();
                    }
                }
            }

            return pagedResult;
        }

        /// <summary>
        /// Override CreateAsync to automatically create version history
        /// </summary>
        public override async Task<RuleEngineDto> CreateAsync(CreateRuleEngineDto createDto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<RuleEngineEntity>(createDto);

            // Auto-generate RuleCode if not provided
            if (string.IsNullOrWhiteSpace(entity.RuleCode))
                entity.RuleCode = await GenerateRuleCodeAsync(cancellationToken);

            // ── Backend generates ruleJson from visual state (frontend no longer sends it) ──
            entity.RuleJson = RuleJsonBuilder.Build(
                ruleName: entity.RuleName,
                ruleCode: entity.RuleCode,
                isActive: entity.IsEnabled,
                ruleCategory: entity.RuleCategory,
                conditionsJson: entity.ConditionsJson,
                effectJson: entity.EffectJson,
                description: entity.Description);

            var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new Exceptions.ValidationException(
                    "Validation failed for create operation",
                    validationResult.ToDictionary(),
                    Enums.OperationType.Create);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await CreateVersionHistoryAsync(entity, "CREATED", createDto.CreatedBy ?? 0, createDto.ChangeReason, null, cancellationToken);

                // Create rule exclusions if SkipRuleIds are provided
                if (createDto.SkipRuleIds != null && createDto.SkipRuleIds.Any())
                {
                    await CreateRuleExclusionsAsync(entity.Id, createDto.SkipRuleIds, createDto.ExclusionReason, createDto.CreatedBy ?? 0, cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // ✅ FIX: Invalidate cache AFTER successful commit to ensure consistency
                _ruleExecutionService.InvalidateCache(entity.RuleCategory);

                var reloadedEntity = await _repository.GetQueryable()
                    .FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);

                return _mapper.Map<RuleEngineDto>(reloadedEntity!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override UpdateAsync to automatically create version history
        /// </summary>
        public override async Task<RuleEngineDto?> UpdateAsync(int id, UpdateRuleEngineDto updateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            var oldState = new
            {
                entity.RuleName,
                entity.Description,
                entity.RuleJson,
                entity.Priority,
                entity.IsEnabled,
                entity.StopProcessing,  // ✅ Track StopProcessing changes
                entity.RuleCategory
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                string changeType = "UPDATED";
                if (oldState.IsEnabled != updateDto.IsEnabled)
                    changeType = updateDto.IsEnabled ? "ENABLED" : "DISABLED";

                _mapper.Map(updateDto, entity);

                // ✅ Ensure StopProcessing is explicitly set (in case AutoMapper doesn't map it)
                entity.StopProcessing = updateDto.StopProcessing;

                // ── Backend re-generates ruleJson whenever rule is updated ──────────────
                entity.RuleJson = RuleJsonBuilder.Build(
                    ruleName: entity.RuleName,
                    ruleCode: entity.RuleCode,
                    isActive: entity.IsEnabled,
                    ruleCategory: entity.RuleCategory,
                    conditionsJson: entity.ConditionsJson,
                    effectJson: entity.EffectJson,
                    description: entity.Description);

                // Update rule exclusions
                // First, deactivate all existing exclusions for this rule
                var query = _ruleExclusionRepository.GetQueryable()
                    .Where(e => e.AppliedRuleId == id && e.IsActive);

                var existingExclusions = query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
                    ? await query.ToListAsync(cancellationToken)
                    : query.ToList();

                foreach (var exclusion in existingExclusions)
                {
                    exclusion.IsActive = false;
                    exclusion.UpdatedBy = updateDto.UpdatedBy;
                    exclusion.UpdatedDate = DateTime.UtcNow;
                    await _ruleExclusionRepository.UpdateAsync(exclusion, cancellationToken);
                }

                // Create new exclusions if SkipRuleIds are provided
                if (updateDto.SkipRuleIds != null && updateDto.SkipRuleIds.Any())
                {
                    await CreateRuleExclusionsAsync(id, updateDto.SkipRuleIds, updateDto.ExclusionReason, updateDto.UpdatedBy ?? 0, cancellationToken);
                }

                // Explicitly update the entity in repository
                await _repository.UpdateAsync(entity, cancellationToken);

                // Create version history before committing to ensure atomicity
                var changeSummary = GenerateChangeSummary(oldState, entity);
                await CreateVersionHistoryAsync(entity, changeType, updateDto.UpdatedBy ?? 0, updateDto.ChangeReason, changeSummary, cancellationToken);

                // Commit all changes (rule update, exclusions, version history) atomically
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // P0: Invalidate cache after successful rule update
                _ruleExecutionService.InvalidateCache(entity.RuleCategory);

                return _mapper.Map<RuleEngineDto>(entity);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override DeleteAsync to automatically create version history
        /// </summary>
        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;

            var ruleCategory = entity.RuleCategory; // Capture before deletion

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Create version history record before deletion
                // Note: We use 0 for ChangedBy since we don't have user context in delete. 
                // Consider passing user ID through the delete method if needed
                await CreateVersionHistoryAsync(entity, "DELETED", 0, "Rule deleted", null, cancellationToken);

                // Delete the entity
                await _repository.DeleteAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // P0: Invalidate cache after successful rule deletion
                _ruleExecutionService.InvalidateCache(ruleCategory);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Get version history for a specific rule
        /// </summary>
        public async Task<List<RuleVersionHistoryDto>> GetVersionHistoryAsync(int ruleId, CancellationToken cancellationToken = default)
        {
            var history = await _versionHistoryRepository.GetQueryable()
                .Where(v => v.RuleId == ruleId)
                .OrderByDescending(v => v.Version)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<RuleVersionHistoryDto>>(history);
        }

        #region Helper Methods

        /// <summary>
        /// Creates a version history record for the rule
        /// </summary>
        private async Task CreateVersionHistoryAsync(
            RuleEngineEntity rule,
            string changeType,
            int changedBy,
            string? changeReason,
            string? changeSummary = null,
            CancellationToken cancellationToken = default)
        {
            // Get the next version number for this rule
            var latestVersion = await _versionHistoryRepository.GetQueryable()
                .Where(v => v.RuleId == rule.Id)
                .MaxAsync(v => (int?)v.Version, cancellationToken) ?? 0;

            var versionHistory = new RuleVersionHistoryEntity
            {
                RuleId = rule.Id,
                RuleCode = rule.RuleCode,
                Version = latestVersion + 1,
                RuleName = rule.RuleName,
                Description = rule.Description,
                RuleJson = rule.RuleJson,
                Priority = rule.Priority,
                IsEnabled = rule.IsEnabled,
                ChangeType = changeType,
                ChangeReason = changeReason,
                ChangedBy = changedBy,
                ChangedDate = DateTime.UtcNow,
                ChangeSummary = changeSummary
            };

            await _versionHistoryRepository.AddAsync(versionHistory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Generates a summary of changes between old and new state
        /// </summary>
        private string GenerateChangeSummary(object oldState, RuleEngineEntity newState)
        {
            var changes = new List<string>();

            // Skip RuleJson — it is derived from conditionsJson/effectJson and is too verbose for a summary
            var skipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RuleJson" };

            var oldProps = oldState.GetType().GetProperties()
                .Where(p => !skipProperties.Contains(p.Name));
            var newType = newState.GetType();

            foreach (var oldProp in oldProps)
            {
                var oldValue = oldProp.GetValue(oldState);
                var newProp = newType.GetProperty(oldProp.Name);
                if (newProp == null) continue;

                var newValue = newProp.GetValue(newState);
                if (!Equals(oldValue, newValue))
                    changes.Add($"{oldProp.Name}: '{oldValue}' → '{newValue}'");
            }

            return changes.Count > 0 ? string.Join("; ", changes) : "No changes detected";
        }

        /// <summary>
        /// Generates a unique rule code in the format: RULE-YYYYMMDD-XXXX
        /// </summary>
        private async Task<string> GenerateRuleCodeAsync(CancellationToken cancellationToken = default)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var baseCode = $"RULE-{datePrefix}";

            // Get the count of rules created today to generate a sequence number
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            var todayCount = await _repository.GetQueryable()
                .Where(r => r.CreatedDate >= todayStart && r.CreatedDate < todayEnd)
                .CountAsync(cancellationToken);

            var sequenceNumber = (todayCount + 1).ToString("D4"); // 4-digit sequence with leading zeros
            var ruleCode = $"{baseCode}-{sequenceNumber}";

            // Ensure uniqueness (in case of race conditions)
            var exists = await _repository.GetQueryable()
                .AnyAsync(r => r.RuleCode == ruleCode, cancellationToken);

            if (exists)
            {
                // Fallback: Add timestamp milliseconds for uniqueness
                var timestamp = DateTime.UtcNow.ToString("HHmmssffff");
                ruleCode = $"{baseCode}-{timestamp}";
            }

            return ruleCode;
        }

        /// <summary>
        /// ✅ Creates rule exclusions with circular dependency detection
        /// </summary>
        private async Task CreateRuleExclusionsAsync(
            int appliedRuleId,
            List<int> skipRuleIds,
            string? reason,
            int createdBy,
            CancellationToken cancellationToken = default)
        {
            // ✅ FIX: Detect circular exclusions before creating
            foreach (var skipRuleId in skipRuleIds)
            {
                if (await HasCircularExclusionAsync(appliedRuleId, skipRuleId, cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Circular exclusion detected: Rule {appliedRuleId} cannot exclude Rule {skipRuleId} " +
                        $"because it would create a circular dependency. Rule {skipRuleId} already excludes Rule {appliedRuleId} directly or indirectly.");
                }
            }

            foreach (var skipRuleId in skipRuleIds)
            {
                // Skip if trying to exclude self
                if (skipRuleId == appliedRuleId)
                    continue;

                // Check if exclusion already exists (and reactivate if it does)
                var existingExclusion = await _ruleExclusionRepository.GetQueryable()
                    .FirstOrDefaultAsync(e => e.AppliedRuleId == appliedRuleId && e.SkipRuleId == skipRuleId, cancellationToken);

                if (existingExclusion != null)
                {
                    // Reactivate existing exclusion
                    existingExclusion.IsActive = true;
                    existingExclusion.Reason = reason;
                    existingExclusion.UpdatedBy = createdBy;
                    existingExclusion.UpdatedDate = DateTime.UtcNow;
                    await _ruleExclusionRepository.UpdateAsync(existingExclusion, cancellationToken);
                }
                else
                {
                    // Create new exclusion
                    var exclusion = new RuleExclusionEntity
                    {
                        AppliedRuleId = appliedRuleId,
                        SkipRuleId = skipRuleId,
                        Reason = reason,
                        IsActive = true,
                        CreatedBy = createdBy,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _ruleExclusionRepository.AddAsync(exclusion, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// ✅ Detects circular exclusions using graph traversal (BFS)
        /// Returns true if adding "appliedRuleId excludes skipRuleId" would create a cycle
        /// </summary>
        private async Task<bool> HasCircularExclusionAsync(
            int appliedRuleId,
            int skipRuleId,
            CancellationToken cancellationToken = default)
        {
            // Check if skipRuleId already excludes appliedRuleId (direct cycle)
            var directCycle = await _ruleExclusionRepository.GetQueryable()
                .AnyAsync(e => e.AppliedRuleId == skipRuleId &&
                              e.SkipRuleId == appliedRuleId &&
                              e.IsActive,
                          cancellationToken);

            if (directCycle)
                return true;

            // Check for indirect cycles using BFS (Breadth-First Search)
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(skipRuleId);

            while (queue.Count > 0)
            {
                var currentRuleId = queue.Dequeue();

                if (visited.Contains(currentRuleId))
                    continue;

                visited.Add(currentRuleId);

                // If current rule excludes appliedRuleId, we have a cycle
                if (currentRuleId == appliedRuleId)
                    return true;

                // Get all rules that currentRule excludes
                var excludedRules = await _ruleExclusionRepository.GetQueryable()
                    .Where(e => e.AppliedRuleId == currentRuleId && e.IsActive)
                    .Select(e => e.SkipRuleId)
                    .ToListAsync(cancellationToken);

                foreach (var excludedRule in excludedRules)
                {
                    if (!visited.Contains(excludedRule))
                        queue.Enqueue(excludedRule);
                }
            }

            return false;
        }

        /// <summary>
        /// Populates rule exclusion information in the DTO
        /// </summary>
        /// <remarks>
        /// Uses async query when available (EF Core in production), falls back to sync for mock scenarios.
        /// </remarks>
        private async Task PopulateRuleExclusionsAsync(RuleEngineDto dto, CancellationToken cancellationToken = default)
        {
            var query = _ruleExclusionRepository.GetQueryable()
                .Where(e => e.AppliedRuleId == dto.Id && e.IsActive)
                .Include(e => e.SkipRule);

            var exclusions = query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
                ? await query.ToListAsync(cancellationToken)
                : query.ToList();

            dto.SkipRuleIds = exclusions.Select(e => e.SkipRuleId).ToList();
            dto.SkipRules = exclusions.Select(e => new SkipRuleInfo
            {
                RuleId = e.SkipRuleId,
                RuleCode = e.SkipRule.RuleCode,
                RuleName = e.SkipRule.RuleName,
                Reason = e.Reason
            }).ToList();
        }

        #endregion
    }
}
