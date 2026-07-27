using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Client for Rateable Value recalculation API. Delegates to the real <see cref="IRateableValueService"/>
/// to refresh PropertyTaxDetails NETTAX and PropertyTaxCalculationRVResults for the property.
/// This is step 1 of the certificate-change pipeline (runs before Occupation Tax calculation).
/// </summary>
public sealed class RateableValueApiClient : IRateableValueApiClient
{
    private readonly IRateableValueService _rvService;
    private readonly ILogger<RateableValueApiClient> _logger;

    public RateableValueApiClient(IRateableValueService rvService, ILogger<RateableValueApiClient> logger)
    {
        _rvService = rvService ?? throw new ArgumentNullException(nameof(rvService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecalculateAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RV recalculation step 1 (BEFORE Occupation Tax): property {PropertyId}", propertyId);
        try
        {
            await _rvService.CalculateAndSaveAsync(propertyId);
            _logger.LogInformation("RV recalculation completed for property {PropertyId}", propertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RV recalculation failed for property {PropertyId}", propertyId);
            throw;
        }
    }
}
