using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("SmsGatewayDetails", Schema = "CORE")]
public class SmsGatewayDetailsEntity
{
    [Key]
    public int SMSGatewayDetailsID { get; set; }

    public int SMSGatewayMasterID { get; set; }

    [Required]
    [MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;

    public string? Value { get; set; }

    public int? SequenceNo { get; set; }

    public bool IsURL { get; set; } = false;

    public bool IsMessage { get; set; } = false;

    public bool IsMobile { get; set; } = false;

    public bool IsTemplateID { get; set; } = false;

    public bool IsUnicode { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("SMSGatewayMasterID")]
    public virtual SMSGatewayMasterEntity? GatewayMaster { get; set; }
}
