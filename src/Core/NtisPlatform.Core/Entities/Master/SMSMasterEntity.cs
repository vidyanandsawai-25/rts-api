using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("SMSMaster", Schema = "CORE")]
public class SMSMasterEntity
{
    [Key]
    public int SmsID { get; set; }

    public int SMSGatewayMasterID { get; set; }

    public int SMSTypeID { get; set; }

    [Required]
    [MaxLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TemplateID { get; set; }

    [Required]
    public string SmsText { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("SMSGatewayMasterID")]
    public virtual SMSGatewayMasterEntity? GatewayMaster { get; set; }

    [ForeignKey("SMSTypeID")]
    public virtual SMSTypeEntity? SmsType { get; set; }
}
