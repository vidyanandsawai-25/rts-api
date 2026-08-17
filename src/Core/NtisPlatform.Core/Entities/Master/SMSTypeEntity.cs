using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("SMSType", Schema = "CORE")]
public class SMSTypeEntity
{
    [Key]
    public int SMSTypeID { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<SMSMasterEntity> SmsTemplates { get; set; } = new List<SMSMasterEntity>();
}
