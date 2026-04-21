
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    [Table("UserRoleAllocation", Schema = "Core")]
    public class UserRoleAllocationEntity : BaseEntity
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public int UserRoleId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserEntity? User { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public DepartmentMasterEntity? Department { get; set; }

        [ForeignKey(nameof(UserRoleId))]
        public UserRoleMasterEntity? UserRole { get; set; }
    }
}
