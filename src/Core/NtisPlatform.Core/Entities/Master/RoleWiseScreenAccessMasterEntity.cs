namespace NtisPlatform.Core.Entities.Master
{
    /// <summary>
    /// Defines screen-level access permissions for user roles.
    /// Maps user roles to screens with granular CRUD permissions.
    /// </summary>
    public class RoleWiseScreenAccessMasterEntity : BaseEntity
    {
        /// <summary>Primary key for role-wise screen access</summary>
        public int RoleWiseScreenAccessId { get; set; }
        
        /// <summary>Foreign key to UserRoleMaster</summary>
        public int UserRoleId { get; set; }
        
        /// <summary>Foreign key to ScreenMaster</summary>
        public int ScreenId { get; set; }
        
        /// <summary>Permission to view the screen</summary>
        public bool CanView { get; set; }
        
        /// <summary>Permission to edit/update data on the screen</summary>
        public bool CanEdit { get; set; }
        
        /// <summary>Permission to delete data on the screen</summary>
        public bool CanDelete { get; set; }
        
        /// <summary>Full access flag - grants all permissions. Mutually exclusive with HaveNoAccess</summary>
        public bool HaveFullAccess { get; set; }
        
        /// <summary>No access flag - denies all permissions. Mutually exclusive with HaveFullAccess</summary>
        public bool HaveNoAccess { get; set; }

        // Navigation properties
        /// <summary>Navigation property to UserRoleMaster</summary>
        public virtual UserRoleMasterEntity? UserRole { get; set; }
        
        /// <summary>Navigation property to ScreenMaster</summary>
        public virtual ScreenMasterEntity? Screen { get; set; }
    }
}
