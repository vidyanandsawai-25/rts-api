using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for resolving module and department metadata with memory caching.
/// Caches lookups for 10 minutes to reduce repeated database queries.
/// Replaces hardcoded module/department code enums in PropertyCertificateApplicationService,
/// PropertyPhotoApplicationService, and PropertyDiscountDocumentService.
/// </summary>
public class ModuleLookupService : IModuleLookupService
{
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ModuleLookupService> _logger;
    private const int CacheDurationMinutes = 10;

    public ModuleLookupService(
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IMemoryCache cache,
        ILogger<ModuleLookupService> logger)
    {
        _moduleRepository = moduleRepository ?? throw new ArgumentNullException(nameof(moduleRepository));
        _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetModuleCodeByIdAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));

        var cacheKey = $"ModuleCode_{moduleId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedCode))
        {
            _logger.LogDebug("Cache hit for module code: {ModuleId}", moduleId);
            return cachedCode!;
        }

        var module = await _moduleRepository.GetByIdAsync(moduleId, cancellationToken);
        if (module == null || !module.IsActive)
            throw new InvalidOperationException($"Module with ID {moduleId} not found or is inactive.");

        if (string.IsNullOrWhiteSpace(module.ModuleCode))
            throw new InvalidOperationException($"Module {moduleId} has no module code defined.");

        SetCacheWithLimit(cacheKey, module.ModuleCode);
        return module.ModuleCode;
    }

    public async Task<string> GetModuleNameByIdAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));

        var cacheKey = $"ModuleName_{moduleId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedName))
        {
            _logger.LogDebug("Cache hit for module name: {ModuleId}", moduleId);
            return cachedName!;
        }

        var module = await _moduleRepository.GetByIdAsync(moduleId, cancellationToken);
        if (module == null || !module.IsActive)
            throw new InvalidOperationException($"Module with ID {moduleId} not found or is inactive.");

        if (string.IsNullOrWhiteSpace(module.ModuleName))
            throw new InvalidOperationException($"Module {moduleId} has no module name defined.");

        SetCacheWithLimit(cacheKey, module.ModuleName);
        return module.ModuleName;
    }

    public async Task<int> GetModuleIdByCodeAsync(string moduleCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be empty.", nameof(moduleCode));

        var cacheKey = $"ModuleId_{moduleCode.ToUpperInvariant()}";
        if (_cache.TryGetValue(cacheKey, out int cachedId))
        {
            _logger.LogDebug("Cache hit for module ID: {ModuleCode}", moduleCode);
            return cachedId;
        }

        var modules = await _moduleRepository.GetAsync(m => m.IsActive, cancellationToken);

        // Try exact match first (deterministic, ordered by ID)
        var exactMatch = modules
            .Where(m => m.ModuleCode != null)
            .Where(m => m.ModuleCode!.Equals(moduleCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Id)
            .FirstOrDefault();

        if (exactMatch != null)
        {
            SetCacheWithLimit(cacheKey, exactMatch.Id);
            return exactMatch.Id;
        }

        // Fallback to substring match only if it is unambiguous
        var substringMatches = modules
            .Where(m => m.ModuleCode != null)
            .Where(m => m.ModuleCode!.Contains(moduleCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Id)
            .ToList();

        if (substringMatches.Count == 1)
        {
            SetCacheWithLimit(cacheKey, substringMatches[0].Id);
            return substringMatches[0].Id;
        }

        if (substringMatches.Count > 1)
        {
            var ambiguous = string.Join(", ", substringMatches.Select(m => $"{m.ModuleCode} (ID: {m.Id})"));
            throw new InvalidOperationException($"Ambiguous module code '{moduleCode}'. Matches: {ambiguous}");
        }

        var availableCodes = string.Join(", ", modules.Select(m => m.ModuleCode ?? "NULL"));
        throw new InvalidOperationException(
            $"No active module found with code '{moduleCode}'. Available modules: {availableCodes}");
    }

    public async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleAsync(
        string departmentCode,
        string moduleCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
            throw new ArgumentException("Department code cannot be empty.", nameof(departmentCode));

        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be empty.", nameof(moduleCode));

        var cacheKey = $"DeptModule_{departmentCode.ToUpperInvariant()}_{moduleCode.ToUpperInvariant()}";
        if (_cache.TryGetValue(cacheKey, out (int DepartmentId, int ModuleId) cachedResult))
        {
            _logger.LogDebug("Cache hit for department/module: {DeptCode}/{ModuleCode}", departmentCode, moduleCode);
            return cachedResult;
        }

        // Resolve department
        var departments = await _departmentRepository.GetAsync(d => d.IsActive, cancellationToken);

        var exactDeptMatch = departments
            .Where(d => d.DepartmentCode != null)
            .Where(d => d.DepartmentCode!.Equals(departmentCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Id)
            .FirstOrDefault();

DepartmentMasterEntity? department = exactDeptMatch;
if (department == null)
{
    // Fallback to substring match only if unambiguous
    var substringDeptMatches = departments
        .Where(d => d.DepartmentCode != null)
        .Where(d => d.DepartmentCode!.Contains(departmentCode, StringComparison.OrdinalIgnoreCase))
        .OrderBy(d => d.Id)
        .ToList();

    if (substringDeptMatches.Count == 1)
        department = substringDeptMatches[0];
    else if (substringDeptMatches.Count > 1)
        throw new InvalidOperationException(
            $"Ambiguous department code '{departmentCode}'. Matches: {string.Join(", ", substringDeptMatches.Select(d => $"{d.DepartmentCode} (ID: {d.Id})"))}");
}

        if (department == null)
        {
            var availableDepts = string.Join(", ", departments.Select(d => d.DepartmentCode ?? "NULL"));
            throw new InvalidOperationException(
                $"No active department found with code '{departmentCode}'. Available departments: {availableDepts}");
        }

        // Resolve module under the department
        var modules = await _moduleRepository.GetAsync(
            m => m.DepartmentId == department.Id && m.IsActive,
            cancellationToken);

        var exactModuleMatch = modules
            .Where(m => m.ModuleCode != null)
            .Where(m => m.ModuleCode!.Equals(moduleCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Id)
            .FirstOrDefault();

ModuleMasterEntity? module = exactModuleMatch;
if (module == null)
{
    // Fallback to substring match only if unambiguous
    var substringModuleMatches = modules
        .Where(m => m.ModuleCode != null)
        .Where(m => m.ModuleCode!.Contains(moduleCode, StringComparison.OrdinalIgnoreCase))
        .OrderBy(m => m.Id)
        .ToList();

    if (substringModuleMatches.Count == 1)
        module = substringModuleMatches[0];
    else if (substringModuleMatches.Count > 1)
        throw new InvalidOperationException(
            $"Ambiguous module code '{moduleCode}' under department '{departmentCode}'. Matches: {string.Join(", ", substringModuleMatches.Select(m => $"{m.ModuleCode} (ID: {m.Id})"))}");
}

        if (module == null)
        {
            var availableModules = string.Join(", ", modules.Select(m => $"{m.ModuleCode ?? "NULL"} (ID: {m.Id})"));
            throw new InvalidOperationException(
                $"No active module found with code '{moduleCode}' under department '{departmentCode}' (ID: {department.Id}). " +
                $"Available modules: {availableModules}");
        }

        var result = (department.Id, module.Id);
        SetCacheWithLimit(cacheKey, result);

        _logger.LogDebug("Resolved department/module context: {DeptCode} (ID={DeptId}), {ModuleCode} (ID={ModuleId})",
            department.DepartmentCode, department.Id, module.ModuleCode, module.Id);

        return result;
    }

    public async Task<string?> GetReferenceTableNameAsync(
        int moduleId,
        string? referenceTableCode = null,
        CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));

        // For now, this is a stub. In the future, add a ModuleReferenceTable mapping
        // to store the reference table name per module (e.g., PropertyCertificates, PropertyPhoto, etc.)
        // For current implementation, callers should pass the table name directly.

        _logger.LogWarning("GetReferenceTableNameAsync not yet implemented for module {ModuleId}. " +
            "Callers should provide ReferenceTableName directly to DocumentBinding.", moduleId);

        return null;
    }

    public async Task<bool> ModuleExistsAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        if (moduleId <= 0)
            return false;

        var cacheKey = $"ModuleExists_{moduleId}";
        if (_cache.TryGetValue(cacheKey, out bool cachedExists))
        {
            return cachedExists;
        }

        var module = await _moduleRepository.GetByIdAsync(moduleId, cancellationToken);
        var exists = module != null && module.IsActive;

        SetCacheWithLimit(cacheKey, exists);
        return exists;
    }

    public async Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        if (departmentId <= 0)
            return false;

        var cacheKey = $"DepartmentExists_{departmentId}";
        if (_cache.TryGetValue(cacheKey, out bool cachedExists))
        {
            return cachedExists;
        }

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        var exists = department != null && department.IsActive;

        SetCacheWithLimit(cacheKey, exists);
        return exists;
    }

    private void SetCacheWithLimit<T>(string key, T value)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes),
            Size = 1
        });
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
            _logger.LogInformation("ModuleLookupService cache cleared");
        }
    }
}
