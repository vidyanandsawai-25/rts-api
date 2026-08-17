using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("SMSSendDetails", Schema = "CORE")]
public class SMSSendDetailsEntity
{
    [Key]
    public long SMSSendDetailsID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReceiverMobileNo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SenderName { get; set; }

    [MaxLength(100)]
    public string? TemplateID { get; set; }

    public int? SMSTypeID { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? SmsUrl { get; set; }

    [Required]
    [MaxLength(50)]
    public string SMSStatus { get; set; } = "PENDING";

    public string? GatewayResponse { get; set; }

    public int? ApplicationId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
