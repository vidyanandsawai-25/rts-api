using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Read model for a <c>PTIS.ULBDocumentType</c> master row — the category of ULB-wide document
/// (e.g. Tax Zoning List/Map, Ready Reckoner Rate Chart) that <c>PTIS.ULBDocument</c> rows are
/// keyed against.
/// </summary>
public class ULBDocumentTypeDto
{
    public int Id { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateULBDocumentTypeDto
{
    [Required(ErrorMessage = "ULBDocumentType_DocumentTypeCode_Required")]
    [StringLength(100, ErrorMessage = "ULBDocumentType_DocumentTypeCode_MaxLen_100")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ULBDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "ULBDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;
}

/// <summary>
/// <c>DocumentTypeCode</c> is immutable after creation (it's the stable key <c>ULBDocument</c> rows
/// and frontend constants reference) — only the display name and active flag can be updated.
/// </summary>
public class UpdateULBDocumentTypeDto
{
    [Required(ErrorMessage = "ULBDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "ULBDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
