using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    [Table("SocialAttributeMaster", Schema = "PTIS")]
    public class SocialAttributeEntity : BaseEntity
    {
        public string SocialAttributeCode { get; set; } = string.Empty;
        public string SocialAttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public int? ParentAttributeId { get; set; }
        public bool IsRequiredWhenParentTrue { get; set; } = false;
        public bool IsDiscountApplicable { get; set; } = false;
    }
}
