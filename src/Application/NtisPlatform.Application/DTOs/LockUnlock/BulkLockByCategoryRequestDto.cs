using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.LockUnlock;

/// <summary>
/// Bulk lock/unlock request scoped by SearchCategory (Zone/Ward/Building/Range - same scoping
/// concept as PropertySearchByCategory) instead of an explicit PropertyIds list. The server
/// resolves every matching property and applies the action to all of them.
/// </summary>
public class BulkLockByCategoryRequestDto
{
    [Required(ErrorMessage = "BulkLockByCategory_Scope_Required")]
    public BulkLockCategoryScopeDto Scope { get; set; } = new();

    [Required(ErrorMessage = "BulkLockByCategory_ScreenIds_Required")]
    [MinLength(1, ErrorMessage = "BulkLockByCategory_ScreenIds_Required")]
    public List<int> ScreenIds { get; set; } = new();

    [Required(ErrorMessage = "BulkLockByCategory_Action_Required")]
    [RegularExpression("(?i)^(lock|unlock)$", ErrorMessage = "BulkLockByCategory_Action_Invalid")]
    public string Action { get; set; } = "lock";
}
