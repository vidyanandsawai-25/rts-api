namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Entity representing role-wise screen access permissions in the system
/// </summary>
public class RoleWiseScreenAccessMasterEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to UserRoleMaster
    /// </summary>
    public int UserRoleId { get; set; }

    /// <summary>
    /// Foreign key to ScreenMaster
    /// </summary>
    public int ScreenId { get; set; }

    /// <summary>
    /// Whether the role can view this screen
    /// </summary>
    public bool CanView { get; set; }

    /// <summary>
    /// Whether the role can edit on this screen
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Whether the role can delete on this screen
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Whether the role has full access to this screen
    /// </summary>
    public bool HaveFullAccess { get; set; }

    /// <summary>
    /// Whether the role has no access to this screen
    /// </summary>
    public bool HaveNoAccess { get; set; }

    /// <summary>
    /// Navigation property to the user role
    /// </summary>
    public UserRoleMasterEntity? UserRole { get; set; }

    /// <summary>
    /// Navigation property to the screen
    /// </summary>
    public ScreenMasterEntity? Screen { get; set; }
}
