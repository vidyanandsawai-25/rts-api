using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPolicyConfigurationService
    : ICommonCrudService<PolicyConfigurationEntity, PolicyConfigurationDto, CreatePolicyConfigurationDto, UpdatePolicyConfigurationDto, PolicyConfigurationQueryParameters, int>
{
    /// <summary>
    /// Gets the policy value for a given policy code. Returns the default value if policy is not found, inactive, or has a null/empty value.
    /// </summary>
    /// <param name="policyCode">The policy code to look up</param>
    /// <param name="defaultValue">The default value to return if policy is not found or invalid</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy value or the default value</returns>
    Task<string> GetPolicyValueAsync(string policyCode, string defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple policy values for given policy codes in a single call.
    /// </summary>
    /// <param name="policyCodes">Dictionary of policy codes and their default values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of policy codes to their values (or defaults)</returns>
    Task<Dictionary<string, string>> GetPolicyValuesAsync(Dictionary<string, string> policyCodes, CancellationToken cancellationToken = default);
}
