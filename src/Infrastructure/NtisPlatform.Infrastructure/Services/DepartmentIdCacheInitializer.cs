using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Hosted service that populates the DepartmentIdCache on application startup.
/// Runs after all services are ready to ensure database connectivity.
/// </summary>
public class DepartmentIdCacheInitializer : IHostedService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDepartmentIdCache _cache;
    private readonly ILogger<DepartmentIdCacheInitializer> _logger;

    public DepartmentIdCacheInitializer(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IDepartmentIdCache cache,
        ILogger<DepartmentIdCacheInitializer> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using (var context = await _contextFactory.CreateDbContextAsync(cancellationToken))
            {
                // Prefer exact PTIS match, fallback to PROPERTY for legacy data.
                var ptisDept = await context.DepartmentMasters
                    .AsNoTracking()
                    .Where(d => d.IsActive && (d.DepartmentCode == "PTIS" || d.DepartmentCode == "PROPERTY"))
                    .OrderByDescending(d => d.DepartmentCode == "PTIS")
                    .ThenBy(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ptisDept == null)
                {
                    _logger.LogError(
                        "DepartmentIdCacheInitializer: PTIS department not found in DepartmentMaster. " +
                        "Ensure a department with code 'PTIS' or 'PROPERTY' exists and is active.");
                    throw new InvalidOperationException(
                        "PTIS department not found in DepartmentMaster during cache initialization.");
                }

                _cache.SetPtisdepartmentId(ptisDept.Id);
                _logger.LogInformation(
                    "DepartmentIdCacheInitializer: Successfully cached PTIS department ID {DepartmentId}",
                    ptisDept.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DepartmentIdCacheInitializer: Failed to initialize department cache");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}