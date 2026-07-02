using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;

/// <summary>
/// Abstraction for master data loading.
/// Allows for different implementations (cached, non-cached, mocked).
/// </summary>
public interface ICapitalValueMasterDataProvider
{
    /// <summary>
    /// Loads all master data required for CV calculation.
    /// </summary>
    Task<MasterDataContext> LoadMasterDataAsync(int moujaId, string csn, List<int>? propertyDetailsIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an active policy configuration by policy code.
    /// </summary>
    Task<PolicyConfigurationEntity?> LoadPolicyConfigurationAsync(string policyCode, CancellationToken cancellationToken = default);
}
