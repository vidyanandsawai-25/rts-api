using MediatR;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces.TaxEngine;

namespace NtisPlatform.Application.EventHandlers;

/// <summary>
/// Certificate-change pipeline. Runs when a <see cref="PropertyCertificateChangedEvent"/> is
/// published, in a strict two-step order:
/// <list type="number">
///   <item>Refresh the Rateable Value (and therefore PropertyTaxDetails NETTAX and
///   PropertyTaxCalculationRVResults) via the RV API.</item>
///   <item>Apply Occupation Tax using the freshly-computed NETTAX.</item>
/// </list>
/// The RV refresh MUST complete before the Occupation Tax engine runs, otherwise the engine would
/// consume stale NETTAX figures.
/// </summary>
public class PropertyCertificateChangedEventHandler
    : INotificationHandler<PropertyCertificateChangedEvent>
{
    private readonly IRateableValueApiClient _rvClient;
    private readonly IOccupationTaxService _taxService;
    private readonly ILogger<PropertyCertificateChangedEventHandler> _logger;

    public PropertyCertificateChangedEventHandler(
        IRateableValueApiClient rvClient,
        IOccupationTaxService taxService,
        ILogger<PropertyCertificateChangedEventHandler> logger)
    {
        _rvClient = rvClient ?? throw new ArgumentNullException(nameof(rvClient));
        _taxService = taxService ?? throw new ArgumentNullException(nameof(taxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(
        PropertyCertificateChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation(
            "Certificate changed for property {PropertyId} by user {UserId}: refreshing RV then applying Occupation Tax.",
            notification.PropertyId, notification.UserId);

        // STEP 1: Call RV API to refresh PropertyTaxDetails NETTAX + PropertyTaxCalculationRVResults.
        await _rvClient.RecalculateAsync(notification.PropertyId, cancellationToken);

        // STEP 2: Call OccupationTaxService to apply taxes (consumes the refreshed NETTAX).
        await _taxService.ApplyAsync(
            notification.PropertyId, notification.UserId, cancellationToken);

        _logger.LogInformation(
            "Certificate-change pipeline completed for property {PropertyId}.",
            notification.PropertyId);
    }
}
