using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.RuleEngine;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.RuleEngine;
using NtisPlatform.Core.Models.RuleEngine;

namespace NtisPlatform.Application.Services.RuleEngine
{
    public class RuleFieldsService : BaseCommonCrudService<RulesFieldEntity, RuleFieldsDto, CreateRuleFieldsDto, UpdateRuleFieldsDto, RuleFieldsQueryParameters, int>, IRuleFieldsService
    {
        private readonly IRuleFieldsRepository _ruleFieldsRepository;
        private readonly IRepository<FieldConfigurationEntity, int> _configRepository;
        private readonly IRepository<RuleScopeFieldMappingEntity, int> _scopeMappingRepository;

        public RuleFieldsService(
            IRepository<RulesFieldEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRuleFieldsRepository ruleFieldsRepository,
            IRepository<FieldConfigurationEntity, int> configRepository,
            IRepository<RuleScopeFieldMappingEntity, int> scopeMappingRepository)
            : base(repository, unitOfWork, mapper)
        {
            _ruleFieldsRepository = ruleFieldsRepository;
            _configRepository = configRepository;
            _scopeMappingRepository = scopeMappingRepository;
        }

        /// <summary>
        /// Override GetAllAsync to eagerly load FieldConfiguration for the left join.
        /// This implements the SQL query: SELECT RF.*, FC.* FROM RulesFieldMaster RF LEFT JOIN FieldConfiguration FC ON RF.Id = FC.RulesFieldId
        /// </summary>
        public override async Task<PagedResult<RuleFieldsDto>> GetAllAsync(RuleFieldsQueryParameters queryParameters, CancellationToken cancellationToken = default)
        {
            // Start with queryable
            IQueryable<RulesFieldEntity> query = _repository.GetQueryable();

            // Apply filters
            query = query.ApplyFilters(queryParameters);

            // Apply search
            query = query.ApplySearch(queryParameters);

            // Apply sorting
            query = query.ApplySort(queryParameters);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            List<RulesFieldEntity> entities;
            int pageNumber;
            int pageSize;

            if (queryParameters.PageSize == -1)
            {
                // No pagination - return all records with Include
                entities = await query
                    .Include(x => x.FieldConfiguration)
                    .ToListAsync(cancellationToken);
                pageNumber = 1;
                pageSize = totalCount == 0 ? 1 : totalCount;
            }
            else
            {
                // Paginated results with Include
                entities = await query
                    .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                    .Take(queryParameters.PageSize)
                    .Include(x => x.FieldConfiguration)
                    .ToListAsync(cancellationToken);
                pageNumber = queryParameters.PageNumber;
                pageSize = queryParameters.PageSize;
            }

            // Filter out inactive FieldConfiguration (soft-deleted)
            foreach (var entity in entities)
            {
                if (entity.FieldConfiguration != null && !entity.FieldConfiguration.IsActive)
                {
                    entity.FieldConfiguration = null;
                }
            }

            // Map to DTOs
            var items = _mapper.Map<List<RuleFieldsDto>>(entities);

            return new PagedResult<RuleFieldsDto>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Override GetByIdAsync to eagerly load FieldConfiguration for the left join.
        /// This implements the SQL query: SELECT RF.*, FC.* FROM RulesFieldMaster RF LEFT JOIN FieldConfiguration FC ON RF.Id = FC.RulesFieldId WHERE RF.Id = @id
        /// </summary>
        public override async Task<RuleFieldsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Where(x => x.Id == id)
                .Include(x => x.FieldConfiguration)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
                return null;

            // Filter out inactive FieldConfiguration (soft-deleted)
            if (entity.FieldConfiguration != null && !entity.FieldConfiguration.IsActive)
            {
                entity.FieldConfiguration = null;
            }

            return _mapper.Map<RuleFieldsDto>(entity);
        }

        /// <summary>
        /// Override CreateAsync to handle creation of both RulesField and its FieldConfiguration.
        /// </summary>
        public override async Task<RuleFieldsDto> CreateAsync(CreateRuleFieldsDto createDto, CancellationToken cancellationToken = default)
        {
            // Map and create the main entity
            var entity = _mapper.Map<RulesFieldEntity>(createDto);

            // Validate main entity
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
                // Create the main RulesField entity
                await _repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create FieldConfiguration if configuration data is provided
                if (HasConfigurationData(createDto))
                {
                    FieldConfigurationEntity configuration;

                    if (createDto.FieldConfiguration != null)
                    {
                        // Use nested configuration (preferred)
                        configuration = _mapper.Map<FieldConfigurationEntity>(createDto.FieldConfiguration);
                    }
                    else
                    {
                        // Fallback to flattened properties (backward compatibility)
                        configuration = _mapper.Map<FieldConfigurationEntity>(createDto);
                    }

                    configuration.RulesFieldId = entity.Id; // Set the FK
                    await _configRepository.AddAsync(configuration, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // Create RuleScopeFieldMapping if ruleScopeId is provided
                if (createDto.RuleScopeId.HasValue)
                {
                    var mapping = new RuleScopeFieldMappingEntity
                    {
                        RuleScopeId = createDto.RuleScopeId.Value,
                        RulesFieldId = entity.Id,
                        DisplayOrder = 1,
                        IsActive = true,
                        CreatedBy = createDto.CreatedBy ?? 1,
                        CreatedDate = DateTime.Now
                    };
                    await _scopeMappingRepository.AddAsync(mapping, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Reload with configuration included
                var entityWithConfig = await _repository.GetQueryable()
                    .Where(x => x.Id == entity.Id)
                    .Include(x => x.FieldConfiguration)
                    .FirstOrDefaultAsync(cancellationToken);

                // Filter out inactive FieldConfiguration (soft-deleted)
                if (entityWithConfig?.FieldConfiguration != null && !entityWithConfig.FieldConfiguration.IsActive)
                {
                    entityWithConfig.FieldConfiguration = null;
                }

                return _mapper.Map<RuleFieldsDto>(entityWithConfig!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override UpdateAsync to handle updates of both RulesField and its FieldConfiguration.
        /// </summary>
        public override async Task<RuleFieldsDto?> UpdateAsync(int id, UpdateRuleFieldsDto updateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Where(x => x.Id == id)
                .Include(x => x.FieldConfiguration)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
                return null;

            // Filter out inactive FieldConfiguration (soft-deleted) before processing
            if (entity.FieldConfiguration != null && !entity.FieldConfiguration.IsActive)
            {
                entity.FieldConfiguration = null;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Update the main RulesField entity
                _mapper.Map(updateDto, entity);
                await _repository.UpdateAsync(entity, cancellationToken);

                // Handle FieldConfiguration
                if (HasConfigurationData(updateDto))
                {
                    if (entity.FieldConfiguration != null)
                    {
                        // Update existing configuration
                        if (updateDto.FieldConfiguration != null)
                        {
                            // Use nested configuration (preferred)
                            _mapper.Map(updateDto.FieldConfiguration, entity.FieldConfiguration);
                        }
                        else
                        {
                            // Fallback to flattened properties (backward compatibility)
                            _mapper.Map(updateDto, entity.FieldConfiguration);
                        }
                        await _configRepository.UpdateAsync(entity.FieldConfiguration, cancellationToken);
                    }
                    else
                    {
                        // Create new configuration
                        FieldConfigurationEntity configuration;

                        if (updateDto.FieldConfiguration != null)
                        {
                            // Use nested configuration (preferred)
                            configuration = _mapper.Map<FieldConfigurationEntity>(updateDto.FieldConfiguration);
                        }
                        else
                        {
                            // Fallback to flattened properties (backward compatibility)
                            configuration = _mapper.Map<FieldConfigurationEntity>(updateDto);
                        }

                        configuration.RulesFieldId = id; // Set the FK
                        await _configRepository.AddAsync(configuration, cancellationToken);
                    }
                }
                else if (entity.FieldConfiguration != null)
                {
                    // If no configuration data provided but configuration exists, delete it
                    await _configRepository.DeleteAsync(entity.FieldConfiguration, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Reload with configuration included
                var entityWithConfig = await _repository.GetQueryable()
                    .Where(x => x.Id == id)
                    .Include(x => x.FieldConfiguration)
                    .FirstOrDefaultAsync(cancellationToken);

                // Filter out inactive FieldConfiguration (soft-deleted)
                if (entityWithConfig?.FieldConfiguration != null && !entityWithConfig.FieldConfiguration.IsActive)
                {
                    entityWithConfig.FieldConfiguration = null;
                }

                return _mapper.Map<RuleFieldsDto>(entityWithConfig!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override DeleteAsync to handle deletion of both RulesField and its FieldConfiguration.
        /// </summary>
        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Where(x => x.Id == id)
                .Include(x => x.FieldConfiguration)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
                return false;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete scope mappings first to avoid constraint violation
                var mappings = await _scopeMappingRepository.GetQueryable()
                    .Where(m => m.RulesFieldId == id && m.IsActive)
                    .ToListAsync(cancellationToken);
                foreach (var mapping in mappings)
                {
                    await _scopeMappingRepository.DeleteAsync(mapping, cancellationToken);
                }

                // Delete configuration first (if exists) to avoid foreign key constraint issues
                if (entity.FieldConfiguration != null)
                {
                    await _configRepository.DeleteAsync(entity.FieldConfiguration, cancellationToken);
                }

                // Delete the main entity
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
        /// Get fields by RuleScopeId with configuration details (original method from repository)
        /// </summary>
        public async Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default)
        {
            return await _ruleFieldsRepository.GetByFieldIdAsync(RuleScopeId, cancellationToken);
        }

        #region Helper Methods

        /// <summary>
        /// Checks if the DTO contains configuration data (either nested or flattened)
        /// </summary>
        private bool HasConfigurationData(CreateRuleFieldsDto dto)
        {
            return dto.FieldConfiguration != null
                || !string.IsNullOrEmpty(dto.DataType)
                || !string.IsNullOrEmpty(dto.InputType)
                || dto.HasApiSource.HasValue
                || !string.IsNullOrEmpty(dto.ApiEndpoint)
                || !string.IsNullOrEmpty(dto.ApiMethod)
                || !string.IsNullOrEmpty(dto.ApiParameters)
                || !string.IsNullOrEmpty(dto.ApiResponseMapping)
                || dto.HasStaticValues.HasValue
                || !string.IsNullOrEmpty(dto.StaticValuesJson)
                || dto.IsRequired.HasValue
                || !string.IsNullOrEmpty(dto.DefaultValue)
                || !string.IsNullOrEmpty(dto.ValidationRegex)
                || dto.MinValue.HasValue
                || dto.MaxValue.HasValue
                || dto.MinLength.HasValue
                || dto.MaxLength.HasValue;
        }

        /// <summary>
        /// Checks if the DTO contains configuration data (either nested or flattened)
        /// </summary>
        private bool HasConfigurationData(UpdateRuleFieldsDto dto)
        {
            return dto.FieldConfiguration != null
                || !string.IsNullOrEmpty(dto.DataType)
                || !string.IsNullOrEmpty(dto.InputType)
                || dto.HasApiSource.HasValue
                || !string.IsNullOrEmpty(dto.ApiEndpoint)
                || !string.IsNullOrEmpty(dto.ApiMethod)
                || !string.IsNullOrEmpty(dto.ApiParameters)
                || !string.IsNullOrEmpty(dto.ApiResponseMapping)
                || dto.HasStaticValues.HasValue
                || !string.IsNullOrEmpty(dto.StaticValuesJson)
                || dto.IsRequired.HasValue
                || !string.IsNullOrEmpty(dto.DefaultValue)
                || !string.IsNullOrEmpty(dto.ValidationRegex)
                || dto.MinValue.HasValue
                || dto.MaxValue.HasValue
                || dto.MinLength.HasValue
                || dto.MaxLength.HasValue;
        }

        #endregion
    }
}
