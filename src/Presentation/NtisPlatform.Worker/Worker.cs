using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Worker;

/// <summary>
/// Background worker service for long-running tasks
/// Can be deployed as a Windows Service
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NTIS Platform Worker Service starting at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Worker processing background tasks at: {time}", DateTimeOffset.Now);

                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    // TODO: Add your background processing logic here
                    // Example: var sampleService = scope.ServiceProvider.GetRequiredService<ISampleService>();
                    // var items = await sampleService.GetAllAsync(stoppingToken);
                    // Process items...

                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background processing");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("NTIS Platform Worker Service stopping at: {time}", DateTimeOffset.Now);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NTIS Platform Worker Service started");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NTIS Platform Worker Service stopped");
        return base.StopAsync(cancellationToken);
    }
}
