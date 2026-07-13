namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for resolving module and department metadata.
/// Provides cached, read-only lookups to support document binding and authorization flows.
/// Replaces hardcoded module/department code enums with table-driven configuration.
/// </summary>
public interface IModuleLookupService
{
    /// <summary>
    /// Get module code by module ID.
    /// Example: GetModuleCodeByIdAsync(12) → "PROPERTY"
    /// </summary>
    Task<string> GetModuleCodeByIdAsync(int moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get module name by module ID.
    /// </summary>
    Task<string> GetModuleNameByIdAsync(int moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get module ID by code (supports exact and partial matching).
    /// Example: GetModuleIdByCodeAsync("PROPERTY") → 12
    /// </summary>
    Task<int> GetModuleIdByCodeAsync(string moduleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get department ID and module ID for a specific document context.
    /// Used during document binding creation to resolve auth context.
    /// Example: GetDepartmentAndModuleAsync("PTIS", "PROPERTY") → (DepartmentId: 3, ModuleId: 12)
    /// </summary>
    Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleAsync(
        string departmentCode,
        string moduleCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reference table name for a module (e.g., "PropertyCertificates" for PropertyCertificate module).
    /// Used to validate and resolve DocumentBinding.ReferenceTableName.
    /// </summary>
    Task<string?> GetReferenceTableNameAsync(
        int moduleId,
        string? referenceTableCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a module exists and is active.
    /// </summary>
    Task<bool> ModuleExistsAsync(int moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a department exists and is active.
    /// </summary>
    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the lookup cache (useful for testing or admin operations).
    /// </summary>
    void ClearCache();
}
