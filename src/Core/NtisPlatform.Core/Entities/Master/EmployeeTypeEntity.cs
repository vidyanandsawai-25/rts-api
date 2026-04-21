using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    [Table("EmployeeTypeMaster", Schema = "Core")]
    public class EmployeeTypeEntity : BaseEntity
    {
        public string EmployeeType { get; set; } = string.Empty;
    }
}
