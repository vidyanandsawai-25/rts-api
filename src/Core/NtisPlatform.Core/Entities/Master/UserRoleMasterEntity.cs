using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities
{
    public class UserRoleMasterEntity : BaseEntity
    {
        public int UserRoleId { get; set; } = 0;
        
        [Required]
        [StringLength(100)]
        public string UserRoleName { get; set; } = string.Empty;
    }
}
