using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.EventHandlers;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Interfaces.TaxEngine;
using Xunit;

namespace NtisPlatform.Tests.Application.EventHandlers;

/// <summary>
/// Covers the fire-and-forget conversion: Handle must enqueue the RV-refresh-then-retro-engine
/// pipeline and return immediately, never call either service inline within the same call stack --
/// and the queued work item itself, once invoked by the background hosted service, must still run
/// the two steps in the same strict order as before.
/// </summary>
public class PropertyCertificateChangedEventHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesWorkItem_NeverCallsRvOrRetroEngineInline()
    {
        var mockQueue = new Mock<IBackgroundTaskQueue>();
        Func<IServiceProvider, CancellationToken, Task>? capturedWorkItem = null;
        mockQueue.Setup(q => q.QueueWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()))
            .Callback<Func<IServiceProvider, CancellationToken, Task>>(w => capturedWorkItem = w);

        var mockRvClient = new Mock<IRateableValueApiClient>();
        var mockRetroEngine = new Mock<IRetrospectiveTaxCalculationEngineService>();

        var services = new ServiceCollection();
        services.AddSingleton(mockRvClient.Object);
        services.AddSingleton(mockRetroEngine.Object);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<PropertyCertificateChangedEventHandler>>(
            NullLogger<PropertyCertificateChangedEventHandler>.Instance);
        // A handler built from THIS provider is never used to call Handle -- it exists only so
        // the captured work item (below) has real services to resolve when invoked manually.
        var serviceProvider = services.BuildServiceProvider();

        var handler = new PropertyCertificateChangedEventHandler(mockQueue.Object, NullLogger<PropertyCertificateChangedEventHandler>.Instance);

        await handler.Handle(new PropertyCertificateChangedEvent(PropertyId: 500, UserId: 7), CancellationToken.None);

        mockQueue.Verify(q => q.QueueWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()), Times.Once);
        mockRvClient.Verify(c => c.RecalculateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRetroEngine.Verify(e => e.CalculateAndSaveAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotNull(capturedWorkItem);

        serviceProvider.Dispose();
    }

    [Fact]
    public async Task Handle_QueuedWorkItem_CallsRvRefreshThenRetroEngineInOrder_UsingBackgroundScopeServices()
    {
        const int propertyId = 500;
        const int userId = 7;
        var callOrder = new List<string>();

        var mockRvClient = new Mock<IRateableValueApiClient>();
        mockRvClient.Setup(c => c.RecalculateAsync(propertyId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("RV"))
            .Returns(Task.CompletedTask);

        var mockRetroEngine = new Mock<IRetrospectiveTaxCalculationEngineService>();
        mockRetroEngine.Setup(e => e.CalculateAndSaveAsync(propertyId, userId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Retro"))
            .ReturnsAsync((RetrospectiveTaxEngineResultDto?)null);

        var services = new ServiceCollection();
        services.AddSingleton(mockRvClient.Object);
        services.AddSingleton(mockRetroEngine.Object);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<PropertyCertificateChangedEventHandler>>(
            NullLogger<PropertyCertificateChangedEventHandler>.Instance);
        using var serviceProvider = services.BuildServiceProvider();

        var mockQueue = new Mock<IBackgroundTaskQueue>();
        Func<IServiceProvider, CancellationToken, Task>? capturedWorkItem = null;
        mockQueue.Setup(q => q.QueueWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()))
            .Callback<Func<IServiceProvider, CancellationToken, Task>>(w => capturedWorkItem = w);

        var handler = new PropertyCertificateChangedEventHandler(mockQueue.Object, NullLogger<PropertyCertificateChangedEventHandler>.Instance);
        await handler.Handle(new PropertyCertificateChangedEvent(propertyId, userId), CancellationToken.None);

        Assert.NotNull(capturedWorkItem);

        // Simulate QueuedHostedService invoking the captured work item in a fresh scope.
        await capturedWorkItem!(serviceProvider, CancellationToken.None);

        Assert.Equal(new[] { "RV", "Retro" }, callOrder);
        mockRvClient.Verify(c => c.RecalculateAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        mockRetroEngine.Verify(e => e.CalculateAndSaveAsync(propertyId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
