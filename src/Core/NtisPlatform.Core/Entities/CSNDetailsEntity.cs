using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities
{
    public class CSNDetailsEntity : BaseEntity
    {
        public int RateCVMasterId { get; set; }
        public int MoujaId { get; set; }

        [Required]
        [StringLength(200)]
        public string CSN { get; set; } = string.Empty;
    }
}
