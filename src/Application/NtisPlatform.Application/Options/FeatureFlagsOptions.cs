namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration options for feature flags.
/// Bound to the "FeatureFlags" section in appsettings.json.
/// </summary>
public class FeatureFlagsOptions
{
    /// <summary>
    /// Allows property deletion without payment transaction validation.
    /// </summary>
    /// <remarks>
    /// <para><strong>⚠️ SECURITY WARNING:</strong> This flag should ONLY be enabled in Development environments.</para>
    /// <para>When enabled, properties can be deleted even if payment validation is not yet implemented.</para>
    /// <para>When disabled (default), property deletion is blocked until payment validation is implemented.</para>
    /// <para><strong>Production Safety:</strong> This should always be false in production environments until 
    /// payment transaction validation is fully implemented and tested.</para>
    /// </remarks>
    public bool AllowPropertyDeletionWithoutPaymentValidation { get; set; } = false;
}
