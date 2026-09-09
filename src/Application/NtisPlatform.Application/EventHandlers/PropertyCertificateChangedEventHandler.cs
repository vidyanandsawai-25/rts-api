using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Interfaces.TaxEngine;

namespace NtisPlatform.Application.EventHandlers;

/// <summary>
/// Certificate-change pipeline. Runs when a <see cref="PropertyCertificateChangedEvent"/> is
/// published -- queued as a background work item (fire-and-forget) rather than run inline, so the
/// certificate-save HTTP request returns as soon as the certificate row itself is committed instead
/// of waiting for RV refresh + retro recalculation to finish. This matters most for
/// Society/Wing-scoped certificate saves (see PropertyCertificateApplicationService.SaveCertificateAsync),
/// which can trigger this pipeline for every unit under a wing/society in one request.
/// <para>
/// The queued work runs in its own fresh DI scope (never the enqueueing request's scope, which is
/// disposed as soon as the response is sent) and, once it starts, always runs the two steps in this
/// strict order:
/// <list type="number">
///   <item>Refresh the Rateable Value (and therefore PropertyTaxDetails NETTAX and
///   PropertyTaxCalculationRVResults) via the RV API.</item>
///   <item>Run the Retrospective Rule Engine using the freshly-computed NETTAX (replaces the
///   retired Occupation Tax engine/CertificateTaxGuideline — same auto-recalculate-on-certificate-
///   change trigger, now evaluated against RetrospectiveRuleMaster rules).</item>
/// </list>
/// The RV refresh MUST complete before the retrospective engine runs, otherwise it would consume
/// stale NETTAX figures.
/// </para>
/// </summary>
public class PropertyCertificateChangedEventHandler
    : INotificationHandler<PropertyCertificateChangedEvent>
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<PropertyCertificateChangedEventHandler> _logger;

    public PropertyCertificateChangedEventHandler(
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<PropertyCertificateChangedEventHandler> logger)
    {
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task Handle(
        PropertyCertificateChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var propertyId = notification.PropertyId;
        var userId = notification.UserId;

        _logger.LogInformation(
            "Certificate changed for property {PropertyId} by user {UserId}: queuing RV refresh + Retrospective Rule Engine run.",
            propertyId, userId);

        // The closure below captures only plain data (propertyId, userId) -- every service it uses
        // is resolved from the background scope handed to it at execution time, never from this
        // request's own (about-to-be-disposed) scope. Its cancellationToken is the hosted service's
        // own long-lived token, not this request's -- using the request's would cancel the pipeline
        // the instant the HTTP response is sent.
        _backgroundTaskQueue.QueueWorkItem(async (serviceProvider, backgroundCancellationToken) =>
        {
            var rvClient = serviceProvider.GetRequiredService<IRateableValueApiClient>();
            var retrospectiveTaxEngine = serviceProvider.GetRequiredService<IRetrospectiveTaxCalculationEngineService>();
            var logger = serviceProvider.GetRequiredService<ILogger<PropertyCertificateChangedEventHandler>>();

            // STEP 1: Call RV API to refresh PropertyTaxDetails NETTAX + PropertyTaxCalculationRVResults.
            await rvClient.RecalculateAsync(propertyId, backgroundCancellationToken);

            // STEP 2: Run the Retrospective Rule Engine (consumes the refreshed NETTAX). Returns
            // null when no configured rule's evidence conditions match this property's
            // certificates -- not an error, just nothing to compute (mirrors the retired engine's
            // own "no certificate at all, no fallback configured" outcome).
            await retrospectiveTaxEngine.CalculateAndSaveAsync(propertyId, userId, backgroundCancellationToken);

            logger.LogInformation(
                "Certificate-change pipeline completed for property {PropertyId}.", propertyId);
        });

        return Task.CompletedTask;
    }
}
