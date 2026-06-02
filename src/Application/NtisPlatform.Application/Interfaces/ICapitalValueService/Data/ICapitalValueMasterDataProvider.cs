using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;

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
    Task<MasterDataContext> LoadMasterDataAsync( int moujaId,  string csn,  CancellationToken cancellationToken = default);
}
