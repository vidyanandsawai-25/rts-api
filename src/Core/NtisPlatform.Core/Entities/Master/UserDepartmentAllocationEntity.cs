using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;
[Table("UserDepartmentAllocation", Schema = "Core")]
public class UserDepartmentAllocationEntity : BaseEntity
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public DepartmentMasterEntity? Department { get; set; }
}