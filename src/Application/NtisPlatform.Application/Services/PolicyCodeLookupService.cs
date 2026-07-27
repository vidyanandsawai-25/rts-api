using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PolicyCodeLookupService : IPolicyCodeLookupService
{
    private readonly IRepository<PolicyCodeMasterEntity, int> _repository;

    public PolicyCodeLookupService(IRepository<PolicyCodeMasterEntity, int> repository)
    {
        _repository = repository;
    }

    public async Task<int> GetIdAsync(string policyCode, CancellationToken cancellationToken = default)
    {
        var id = await _repository.GetQueryable()
            .Where(p => p.PolicyCode == policyCode && p.IsActive)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!id.HasValue)
        {
            throw new InvalidOperationException(
                $"No active PTIS.PolicyCodeMaster row found for policy code '{policyCode}'. Seed this master data first.");
        }

        return id.Value;
    }

    public async Task<Dictionary<string, int>> GetIdsAsync(IEnumerable<string> policyCodes, CancellationToken cancellationToken = default)
    {
        var codes = policyCodes.Distinct().ToList();

        var found = await _repository.GetQueryable()
            .Where(p => codes.Contains(p.PolicyCode) && p.IsActive)
            .Select(p => new { p.PolicyCode, p.Id })
            .ToDictionaryAsync(p => p.PolicyCode, p => p.Id, cancellationToken);

        var missing = codes.Where(c => !found.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"No active PTIS.PolicyCodeMaster row found for policy code(s): {string.Join(", ", missing)}. Seed this master data first.");
        }

        return found;
    }

    public async Task<Dictionary<string, int>> GetExistingIdsAsync(IEnumerable<string> policyCodes, CancellationToken cancellationToken = default)
    {
        var codes = policyCodes.Distinct().ToList();

        return await _repository.GetQueryable()
            .Where(p => codes.Contains(p.PolicyCode) && p.IsActive)
            .Select(p => new { p.PolicyCode, p.Id })
            .ToDictionaryAsync(p => p.PolicyCode, p => p.Id, cancellationToken);
    }
}
