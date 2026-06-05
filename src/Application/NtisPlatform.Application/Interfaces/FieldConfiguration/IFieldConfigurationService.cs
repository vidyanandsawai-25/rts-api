using NtisPlatform.Application.DTOs.FieldConfiguration;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.FieldConfiguration
{
    /// <summary>
    /// Service interface for field configuration operations
    /// </summary>
    public interface IFieldConfigurationService : ICommonCrudService<FieldConfigurationEntity, FieldConfigurationDto, CreateFieldConfigurationDto, UpdateFieldConfigurationDto, FieldConfigurationQueryParameters, int>
    {
        /// <summary>
        /// Get field configuration by RulesFieldId
        /// </summary>
        Task<FieldConfigurationDto?> GetByRulesFieldIdAsync(int rulesFieldId, CancellationToken cancellationToken = default);
    }
}
