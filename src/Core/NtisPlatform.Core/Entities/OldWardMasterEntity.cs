using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("OldWardMaster", Schema = "GSMS")]
public class OldWardMasterEntity : BaseEntity
{
    public string? OldWardNo { get; set; }

    public string? OldZoneName { get; set; }
}