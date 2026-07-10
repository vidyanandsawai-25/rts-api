namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Cache for well-known department IDs to avoid sync DB queries during DI setup.
/// This cache is populated on application startup by a hosted service.
/// </summary>
public interface IDepartmentIdCache
{
    /// <summary>
    /// Gets the cached PTIS (Property Tax) department ID.
    /// Must be called only after cache initialization by DepartmentIdCacheInitializer hosted service.
    /// </summary>
    /// <returns>The PTIS department ID from cache.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if cache has not been initialized or if PTIS department not found in database.
    /// Indicates DepartmentIdCacheInitializer hosted service has not completed startup.
    /// </exception>
    int GetPtisdepartmentId();

    /// <summary>
    /// Sets the PTIS department ID (called by startup hosted service).
    /// </summary>
    void SetPtisdepartmentId(int departmentId);

    /// <summary>
    /// Gets whether the cache has been initialized from the database.
    /// </summary>
    bool IsInitialized { get; }
}
