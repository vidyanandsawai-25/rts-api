using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Options;

namespace NtisPlatform.Tests.Helpers;

/// <summary>
/// Helper class for creating feature flag mocks in unit tests.
/// </summary>
public static class FeatureFlagsHelper
{
    /// <summary>
    /// Creates a mock IOptions&lt;FeatureFlagsOptions&gt; with property deletion enabled (default for tests).
    /// </summary>
    public static Mock<IOptions<FeatureFlagsOptions>> CreateWithPropertyDeletionEnabled()
    {
        var mock = new Mock<IOptions<FeatureFlagsOptions>>();
        mock.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });
        return mock;
    }

    /// <summary>
    /// Creates a mock IOptions&lt;FeatureFlagsOptions&gt; with property deletion disabled.
    /// </summary>
    public static Mock<IOptions<FeatureFlagsOptions>> CreateWithPropertyDeletionDisabled()
    {
        var mock = new Mock<IOptions<FeatureFlagsOptions>>();
        mock.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = false
        });
        return mock;
    }

    /// <summary>
    /// Creates a mock IOptions&lt;FeatureFlagsOptions&gt; with custom settings.
    /// </summary>
    public static Mock<IOptions<FeatureFlagsOptions>> CreateWithCustomSettings(bool allowPropertyDeletionWithoutPaymentValidation)
    {
        var mock = new Mock<IOptions<FeatureFlagsOptions>>();
        mock.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = allowPropertyDeletionWithoutPaymentValidation
        });
        return mock;
    }
}
