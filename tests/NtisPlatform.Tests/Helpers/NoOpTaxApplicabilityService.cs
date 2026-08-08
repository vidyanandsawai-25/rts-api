using Moq;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Tests.Helpers;

/// <summary>
/// Shared no-op ITaxApplicabilityService for tests that construct RateableValueService or
/// OccupationTaxApplicationService but don't exercise tax-exemption behavior: reports every
/// property as having no exempted taxes.
/// </summary>
public static class NoOpTaxApplicabilityService
{
    public static ITaxApplicabilityService Instance { get; } = CreateMock().Object;

    public static Mock<ITaxApplicabilityService> CreateMock()
    {
        var mock = new Mock<ITaxApplicabilityService>();
        mock.Setup(t => t.GetExemptedTaxIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HashSet<int>());
        return mock;
    }
}
