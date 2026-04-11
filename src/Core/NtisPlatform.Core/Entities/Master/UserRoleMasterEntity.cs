using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities
{
    public class UserRoleMasterEntity : BaseEntity
    {        
        [Required]
        [StringLength(100)]
        public string UserRoleName { get; set; } = string.Empty;
    }
}
