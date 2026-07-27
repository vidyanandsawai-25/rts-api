using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("OwnerTitleMaster", Schema = "PTIS")]
public class OwnerTitleMasterEntity : BaseEntity
{
    [Column(TypeName = "nvarchar(100)")]
    public string? OwnerTitle { get; set; }
}
