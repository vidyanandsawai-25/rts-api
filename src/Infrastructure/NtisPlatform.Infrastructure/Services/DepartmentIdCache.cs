using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// In-memory cache for well-known department IDs, populated on startup.
/// Eliminates the need for sync DB queries during DI setup.
/// </summary>
public class DepartmentIdCache : IDepartmentIdCache
{
    private int _ptisDepartmentId = 0;
    private bool _initialized = false;

    public int GetPtisdepartmentId()
    {
        if (_ptisDepartmentId <= 0)
            throw new InvalidOperationException(
                "PTIS department ID not cached. Ensure DepartmentIdCacheInitializer hosted service has run.");

        return _ptisDepartmentId;
    }

    public void SetPtisdepartmentId(int departmentId)
    {
        if (departmentId <= 0)
            throw new ArgumentException("Department ID must be greater than zero.", nameof(departmentId));

        _ptisDepartmentId = departmentId;
        _initialized = true;
    }

    public bool IsInitialized => _initialized && _ptisDepartmentId > 0;
}
