using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("SMSGatewayMaster", Schema = "CORE")]
public class SMSGatewayMasterEntity
{
    [Key]
    public int SMSGatewayMasterID { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<SmsGatewayDetailsEntity> GatewayDetails { get; set; } = new List<SmsGatewayDetailsEntity>();
    public virtual ICollection<SMSMasterEntity> SmsTemplates { get; set; } = new List<SMSMasterEntity>();
}
