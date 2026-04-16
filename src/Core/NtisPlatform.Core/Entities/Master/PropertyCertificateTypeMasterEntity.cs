using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents property certificate type master data in the PTIS system
/// </summary>
[Table("PropertyCertificateTypeMaster", Schema = "PTIS")]
public class PropertyCertificateTypeMasterEntity : BaseEntity
{
    [Column(TypeName = "nvarchar(100)")]
    public string CertificateTypeName { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(50)")]
    public string CertificateTypeCode { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(100)")]
    public string FieldCode { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(100)")]
    public string SectionCode { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(50)")]
    public string DocumentTypeCode { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(200)")]
    public string? DisplayLabel { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public bool IsMandatory { get; set; }
}
