using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PolicyConfigurationService
    : BaseCommonCrudService<PolicyConfigurationEntity, PolicyConfigurationDto, CreatePolicyConfigurationDto, UpdatePolicyConfigurationDto, PolicyConfigurationQueryParameters, int>,
      IPolicyConfigurationService
{
    private readonly ILogger<PolicyConfigurationService> _logger;

    public PolicyConfigurationService(
        IRepository<PolicyConfigurationEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PolicyConfigurationService> logger)
        : base(repository, unitOfWork, mapper)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetPolicyValueAsync(string policyCode, string defaultValue, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PolicyCode == policyCode && x.IsActive, cancellationToken);

        if (policy == null)
        {
            _logger.LogWarning("Policy '{PolicyCode}' not found. Using default value '{DefaultValue}'", policyCode, defaultValue);
            return defaultValue;
        }

        if (string.IsNullOrWhiteSpace(policy.PolicyValue))
        {
            _logger.LogWarning("Policy '{PolicyCode}' has null or empty value. Using default value '{DefaultValue}'", policyCode, defaultValue);
            return defaultValue;
        }

        return policy.PolicyValue;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetPolicyValuesAsync(Dictionary<string, string> policyCodes, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>(policyCodes);
        var codes = policyCodes.Keys.ToList();

        var policies = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(x => codes.Contains(x.PolicyCode) && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var code in codes)
        {
            var policy = policies.FirstOrDefault(x => x.PolicyCode == code);
            if (policy == null)
            {
                _logger.LogWarning("Policy '{PolicyCode}' not found. Using default value '{DefaultValue}'", code, policyCodes[code]);
                continue;
            }

            if (string.IsNullOrWhiteSpace(policy.PolicyValue))
            {
                _logger.LogWarning("Policy '{PolicyCode}' has null or empty value. Using default value '{DefaultValue}'", code, policyCodes[code]);
                continue;
            }

            result[code] = policy.PolicyValue;
        }

        return result;
    }
}
