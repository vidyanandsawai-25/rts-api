using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// HYBRID tax strategy configuration (evaluation priority + fallback), backing the
/// HYBRID calculation mode of the Dynamic Tax Register.
/// </summary>
public interface IHybridTaxService
{
    /// <summary>Returns the strategy config for a tax, or defaults if none saved yet.</summary>
    Task<TaxHybridConfigDto> GetConfigAsync(int taxId, CancellationToken cancellationToken = default);

    /// <summary>Insert or update the strategy config for a tax.</summary>
    Task<TaxHybridConfigDto> SaveConfigAsync(TaxHybridConfigDto config, CancellationToken cancellationToken = default);
}
