using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Rules
{
    public interface IPropertyRuleApplicationLogService
    {
        Task<PagedResult<PropertyRuleApplicationLogDto>> GetLogsAsync(PropertyRuleApplicationLogQueryParameters queryParameters, CancellationToken cancellationToken = default);
        Task<PropertyRuleApplicationLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task DeleteByPropertyDetailsIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default);
        Task DeleteByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);
    }
}
