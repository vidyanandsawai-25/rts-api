using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents communication details in the GSMS system.
/// </summary>
[Table("CommunicationDetails", Schema = "GSMS")]
public class CommunicationDetailsEntity : BaseEntity
{
    public int CommunicationTypeId { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string? CommunicationNo { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CommunicationDate { get; set; }

    public int? IssuedBy { get; set; }

    public int? ModuleId { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }

    [Column("DeviceUniqueNo", TypeName = "varchar(100)")]
    public string? DeviceUniqueNo { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Remarks { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string? Status { get; set; }
}