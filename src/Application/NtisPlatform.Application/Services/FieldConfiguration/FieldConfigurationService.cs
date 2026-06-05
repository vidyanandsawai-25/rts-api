using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.FieldConfiguration;
using NtisPlatform.Application.Interfaces.FieldConfiguration;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.FieldConfiguration
{
    /// <summary>
    /// Service for managing field configurations
    /// </summary>
    public class FieldConfigurationService : BaseCommonCrudService<FieldConfigurationEntity, FieldConfigurationDto, CreateFieldConfigurationDto, UpdateFieldConfigurationDto, FieldConfigurationQueryParameters, int>, IFieldConfigurationService
    {
        public FieldConfigurationService(
            IRepository<FieldConfigurationEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }

        /// <summary>
        /// Override ApplyIncludes to eagerly load RulesField navigation property
        /// </summary>
        protected override IQueryable<FieldConfigurationEntity> ApplyIncludes(IQueryable<FieldConfigurationEntity> query)
        {
            return query.Include(x => x.RulesField);
        }

        /// <summary>
        /// Override GetByIdAsync to include RulesField
        /// </summary>
        public override async Task<FieldConfigurationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.RulesField)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            return _mapper.Map<FieldConfigurationDto>(entity);
        }

        /// <summary>
        /// Get field configuration by RulesFieldId
        /// </summary>
        public async Task<FieldConfigurationDto?> GetByRulesFieldIdAsync(int rulesFieldId, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.RulesField)
                .FirstOrDefaultAsync(x => x.RulesFieldId == rulesFieldId, cancellationToken);

            if (entity == null)
                return null;

            return _mapper.Map<FieldConfigurationDto>(entity);
        }
    }
}
