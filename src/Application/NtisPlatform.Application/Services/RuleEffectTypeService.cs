using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RuleEffectTypeService : BaseCommonCrudService<RuleEffectTypeEntity, RuleEffectTypeDto, CreateRuleEffectTypeDto, UpdateRuleEffectTypeDto, RuleEffectTypeQueryParameters, int>, IRuleEffectTypeService
    {
        private readonly IRepository<EffectTypeConfigurationEntity, int> _configRepository;

        public RuleEffectTypeService(
            IRepository<RuleEffectTypeEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRepository<EffectTypeConfigurationEntity, int> configRepository)
            : base(repository, unitOfWork, mapper)
        {
            _configRepository = configRepository;
        }

        /// <summary>
        /// Override GetAllAsync to eagerly load EffectTypeConfiguration for the left join.
        /// This implements the SQL query: SELECT RET.*, ETC.* FROM RuleEffectTypeMaster RET LEFT JOIN EffectTypeConfiguration ETC ON RET.Id = ETC.EffectTypeId
        /// </summary>
        public override async Task<PagedResult<RuleEffectTypeDto>> GetAllAsync(RuleEffectTypeQueryParameters queryParameters, CancellationToken cancellationToken = default)
        {
            // Start with queryable
            IQueryable<RuleEffectTypeEntity> query = _repository.GetQueryable();

            // Apply filters
            query = query.ApplyFilters(queryParameters);

            // Apply search
            query = query.ApplySearch(queryParameters);

            // Apply sorting
            query = query.ApplySort(queryParameters);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            List<RuleEffectTypeEntity> entities;
            int pageNumber;
            int pageSize;

            if (queryParameters.PageSize == -1)
            {
                // No pagination - return all records with Include
                entities = await query
                    .Include(x => x.EffectTypeConfiguration)
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
                    .Include(x => x.EffectTypeConfiguration)
                    .ToListAsync(cancellationToken);
                pageNumber = queryParameters.PageNumber;
                pageSize = queryParameters.PageSize;
            }

            // Map to DTOs
            var items = _mapper.Map<List<RuleEffectTypeDto>>(entities);

            return new PagedResult<RuleEffectTypeDto>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Override GetByIdAsync to eagerly load EffectTypeConfiguration for the left join.
        /// This implements the SQL query: SELECT RET.*, ETC.* FROM RuleEffectTypeMaster RET LEFT JOIN EffectTypeConfiguration ETC ON RET.Id = ETC.EffectTypeId WHERE RET.Id = @id
        /// </summary>
        public override async Task<RuleEffectTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.EffectTypeConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            return _mapper.Map<RuleEffectTypeDto>(entity);
        }

        /// <summary>
        /// Override CreateAsync to handle creation of both RuleEffectType and its EffectTypeConfiguration.
        /// </summary>
        public override async Task<RuleEffectTypeDto> CreateAsync(CreateRuleEffectTypeDto createDto, CancellationToken cancellationToken = default)
        {
            // Map and create the main entity
            var entity = _mapper.Map<RuleEffectTypeEntity>(createDto);

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
                // Create the main RuleEffectType entity
                await _repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create EffectTypeConfiguration if configuration data is provided
                if (HasConfigurationData(createDto))
                {
                    var configuration = _mapper.Map<EffectTypeConfigurationEntity>(createDto);
                    configuration.EffectTypeId = entity.Id; // Set the FK
                    await _configRepository.AddAsync(configuration, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Reload with configuration included
                var entityWithConfig = await _repository.GetQueryable()
                    .Include(x => x.EffectTypeConfiguration)
                    .FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);

                return _mapper.Map<RuleEffectTypeDto>(entityWithConfig!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override UpdateAsync to handle updates of both RuleEffectType and its EffectTypeConfiguration.
        /// </summary>
        public override async Task<RuleEffectTypeDto?> UpdateAsync(int id, UpdateRuleEffectTypeDto updateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.EffectTypeConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Update the main RuleEffectType entity
                _mapper.Map(updateDto, entity);
                await _repository.UpdateAsync(entity, cancellationToken);

                // Handle EffectTypeConfiguration
                if (HasConfigurationData(updateDto))
                {
                    if (entity.EffectTypeConfiguration != null)
                    {
                        // Update existing configuration using AutoMapper
                        _mapper.Map(updateDto, entity.EffectTypeConfiguration);
                        await _configRepository.UpdateAsync(entity.EffectTypeConfiguration, cancellationToken);
                    }
                    else
                    {
                        // Create new configuration using AutoMapper
                        var configuration = _mapper.Map<EffectTypeConfigurationEntity>(updateDto);
                        configuration.EffectTypeId = id; // Set the FK
                        await _configRepository.AddAsync(configuration, cancellationToken);
                    }
                }
                else if (entity.EffectTypeConfiguration != null)
                {
                    // If no configuration data provided but configuration exists, delete it
                    await _configRepository.DeleteAsync(entity.EffectTypeConfiguration, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Reload with configuration included
                var entityWithConfig = await _repository.GetQueryable()
                    .Include(x => x.EffectTypeConfiguration)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                return _mapper.Map<RuleEffectTypeDto>(entityWithConfig!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Override DeleteAsync to handle deletion of both RuleEffectType and its EffectTypeConfiguration.
        /// </summary>
        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.EffectTypeConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return false;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete configuration first (if exists) to avoid foreign key constraint issues
                if (entity.EffectTypeConfiguration != null)
                {
                    await _configRepository.DeleteAsync(entity.EffectTypeConfiguration, cancellationToken);
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

        #region Helper Methods

        /// <summary>
        /// Checks if the DTO contains configuration data
        /// </summary>
        private bool HasConfigurationData(CreateRuleEffectTypeDto dto)
        {
            return !string.IsNullOrEmpty(dto.DataType) || !string.IsNullOrEmpty(dto.InputType) || !string.IsNullOrEmpty(dto.ExpressionTemplate);
        }

        /// <summary>
        /// Checks if the DTO contains configuration data
        /// </summary>
        private bool HasConfigurationData(UpdateRuleEffectTypeDto dto)
        {
            return !string.IsNullOrEmpty(dto.DataType) || !string.IsNullOrEmpty(dto.InputType) || !string.IsNullOrEmpty(dto.ExpressionTemplate);
        }

        #endregion
    }
}
