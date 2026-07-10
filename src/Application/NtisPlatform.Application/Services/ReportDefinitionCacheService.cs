using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Singleton in-memory cache for report definitions and their parameter definitions.
/// Follows the same pattern as ILocalizationService — pre-loaded at startup,
/// invalidated via ReportController after admin changes.
/// Eliminates a DB hit on every report-generate request.
/// </summary>
public class ReportDefinitionCacheService
{
    private volatile IReadOnlyList<ReportDefinitionEntity> _definitions = [];
    private volatile IReadOnlyDictionary<int, IReadOnlyList<ReportParameterDefinitionEntity>> _parameters
        = new Dictionary<int, IReadOnlyList<ReportParameterDefinitionEntity>>();

    private readonly ILogger<ReportDefinitionCacheService> _logger;

    public ReportDefinitionCacheService(ILogger<ReportDefinitionCacheService> logger)
    {
        _logger = logger;
    }

    public void Load(
        IReadOnlyList<ReportDefinitionEntity> definitions,
        IReadOnlyDictionary<int, IReadOnlyList<ReportParameterDefinitionEntity>> parameters)
    {
        _definitions = definitions;
        _parameters = parameters;
        _logger.LogInformation(
            "Report definition cache loaded: {DefCount} definitions, {ParamCount} parameter sets",
            definitions.Count, parameters.Count);
    }

    public void Invalidate()
    {
        _definitions = [];
        _parameters = new Dictionary<int, IReadOnlyList<ReportParameterDefinitionEntity>>();
        _logger.LogInformation("Report definition cache invalidated");
    }

    /// <summary>Returns all cached definitions for list API (no DB hit).</summary>
    public IReadOnlyList<ReportDefinitionEntity> GetAll() => _definitions;

    /// <summary>Finds the definition matching the given report code. Null if not found or inactive.</summary>
    public ReportDefinitionEntity? TryGetDefinition(string reportCode) =>
        _definitions.FirstOrDefault(d =>
            d.ReportCode.Equals(reportCode, StringComparison.OrdinalIgnoreCase)
            && d.IsActive);

    /// <summary>Returns cached parameter definitions for a specific report (ordered by SortOrder).</summary>
    public IReadOnlyList<ReportParameterDefinitionEntity> GetParameters(int reportDefinitionId) =>
        _parameters.TryGetValue(reportDefinitionId, out var list) ? list : [];
}

/// <summary>
/// Hosted service that warms up the report definition + parameter cache at application startup.
/// Re-warms on WarmUpAsync() call (triggered by admin cache-bust endpoint).
/// </summary>
public class ReportDefinitionCacheWarmupService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ReportDefinitionCacheService _cache;
    private readonly ILogger<ReportDefinitionCacheWarmupService> _logger;

    public ReportDefinitionCacheWarmupService(
        IServiceProvider services,
        ReportDefinitionCacheService cache,
        ILogger<ReportDefinitionCacheWarmupService> logger)
    {
        _services = services;
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await WarmUpAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public async Task WarmUpAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = _services.CreateScope();

            var defRepo = scope.ServiceProvider.GetRequiredService<IRepository<ReportDefinitionEntity, int>>();
            var definitions = await defRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.ReportName)
                .ToListAsync(ct);

            var defIds = definitions.Select(d => d.Id).ToList();

            var paramRepo = scope.ServiceProvider.GetRequiredService<IRepository<ReportParameterDefinitionEntity, int>>();
            var allParams = await paramRepo.GetQueryable()
                .AsNoTracking()
                .Where(p => defIds.Contains(p.ReportDefinitionId) && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync(ct);

            var paramsByReport = allParams
                .GroupBy(p => p.ReportDefinitionId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ReportParameterDefinitionEntity>)g.ToList());

            _cache.Load(definitions, paramsByReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm up report definition cache. Reports will still work via DB fallback.");
        }
    }
}
