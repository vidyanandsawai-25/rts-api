using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.DTOs.Rules.RuleCategory;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Service for managing rule engine configurations with automatic versioning
    /// </summary>
    public class RuleEngineService : BaseCommonCrudService<RuleEngineEntity, RuleEngineDto, CreateRuleEngineDto, UpdateRuleEngineDto, RuleEngineQueryParameters, int>, IRuleEngineService
    {
        private readonly IRepository<RuleVersionHistoryEntity, long> _versionHistoryRepository;
        private readonly IRuleExecutionService _ruleExecutionService;

        public RuleEngineService(
            IRepository<RuleEngineEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRepository<RuleVersionHistoryEntity, long> versionHistoryRepository,
            IRuleExecutionService ruleExecutionService)
            : base(repository, unitOfWork, mapper)
        {
            _versionHistoryRepository = versionHistoryRepository;
            _ruleExecutionService = ruleExecutionService;
        }

        /// <summary>
        /// Override to include RuleScope navigation property
        /// </summary>
        protected override IQueryable<RuleEngineEntity> ApplyIncludes(IQueryable<RuleEngineEntity> query)
        {
            return query.Include(r => r.RuleScope);
        }

        /// <summary>
        /// Override GetByIdAsync to include rule exclusions and check MarkedForDeletion
        /// </summary>
        public override async Task<RuleEngineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

            if (entity == null)
                return null;

            var dto = _mapper.Map<RuleEngineDto>(entity);

            return dto;
        }

        public override async Task<Models.PagedResult<RuleEngineDto>> GetAllAsync(
            RuleEngineQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            var query = _repository.GetQueryable()
                .Where(x => !x.MarkedForDeletion);

            // Apply filters
            query = query.ApplyFilters(queryParameters);

            // Apply search
            query = query.ApplySearch(queryParameters);

            // Apply sorting
            query = query.ApplySort(queryParameters);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var pagedQuery = query
                .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

            var queryWithIncludes = ApplyIncludes(pagedQuery);
            List<RuleEngineDto> items;
            if (ReferenceEquals(queryWithIncludes, pagedQuery))
            {
                items = await pagedQuery
                    .ProjectTo<RuleEngineDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var entities = await queryWithIncludes.ToListAsync(cancellationToken);
                items = _mapper.Map<List<RuleEngineDto>>(entities);
            }

            var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
            var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

            return new Models.PagedResult<RuleEngineDto>(items, totalCount, pageNumber, pageSize);
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

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
                entity.StopProcessing,
                entity.RuleScopeId,      // ✅ Track RuleScopeId changes
                entity.RuleCategory
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                string changeType = "UPDATED";
                if (oldState.IsEnabled != updateDto.IsEnabled)
                    changeType = updateDto.IsEnabled ? "ENABLED" : "DISABLED";

                _mapper.Map(updateDto, entity);

                // ✅ Ensure StopProcessing and RuleScopeId are explicitly set
                entity.StopProcessing = updateDto.StopProcessing;
                entity.RuleScopeId    = updateDto.RuleScopeId;

                // ── Backend re-generates ruleJson whenever rule is updated ──────────────
                entity.RuleJson = RuleJsonBuilder.Build(
                    ruleName: entity.RuleName,
                    ruleCode: entity.RuleCode,
                    isActive: entity.IsEnabled,
                    ruleCategory: entity.RuleCategory,
                    conditionsJson: entity.ConditionsJson,
                    effectJson: entity.EffectJson,
                    description: entity.Description);

                // Explicitly update the entity in repository
                await _repository.UpdateAsync(entity, cancellationToken);

                // Create version history before committing to ensure atomicity
                var changeSummary = GenerateChangeSummary(oldState, entity);
                await CreateVersionHistoryAsync(entity, changeType, updateDto.UpdatedBy ?? 0, updateDto.ChangeReason, changeSummary, cancellationToken);

                // Commit all changes (rule update, exclusions, version history) atomically
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
        #endregion
    }
}
