using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    /// <summary>
    /// Represents a rule category master entity (e.g. ARV, ALV, UAV, Depreciation, Surcharges, Exemptions & Deductions, Others)
    /// </summary>
    [Table("RuleCategoryMaster", Schema = "PTIS")]
    public class RuleCategoryEntity : BaseEntity
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}
