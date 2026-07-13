using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

public class CitizenSessionCleanupHostedService : BackgroundService
{
    private readonly ILogger<CitizenSessionCleanupHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public CitizenSessionCleanupHostedService(
        ILogger<CitizenSessionCleanupHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CitizenSessionCleanupHostedService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                _logger.LogInformation("Running expired citizen sessions cleanup...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IRepository<RTSCitizenSessionEntity, int>>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var threshold = DateTime.Now.AddMinutes(-30);

                    // Query active sessions using LINQ
                    var activeSessions = await repository.GetAsync(
                        s => s.IsActive && (s.LastActivityTime ?? s.LoginTime) < threshold,
                        stoppingToken
                    );

                    var expiredSessionsList = activeSessions.ToList();
                    if (expiredSessionsList.Any())
                    {
                        foreach (var session in expiredSessionsList)
                        {
                            session.IsActive = false;
                            session.LogoutTime = session.LastActivityTime ?? session.LoginTime;
                            await repository.UpdateAsync(session, stoppingToken);
                        }

                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Cleaned up {Count} expired citizen sessions using LINQ repository pattern.", expiredSessionsList.Count);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during citizen session cleanup.");
            }
        }

        _logger.LogInformation("CitizenSessionCleanupHostedService stopped.");
    }
}
