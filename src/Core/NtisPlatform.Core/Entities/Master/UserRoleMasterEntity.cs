using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities
{
    public class UserRoleMasterEntity : BaseEntity
    {        
        [Required]
        [StringLength(100)]
        public string UserRoleName { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public DepartmentMasterEntity? Department { get; set; }
    }
}
