using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;
[Table("UserModuleAllocation", Schema = "Core")]
public class UserModuleAllocationEntity : BaseEntity
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    public int ModuleId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public DepartmentMasterEntity? Department { get; set; }

    [ForeignKey(nameof(ModuleId))]
    public ModuleMasterEntity? Module { get; set; }
}