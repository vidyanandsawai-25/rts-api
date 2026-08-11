using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetGrievanceRemarkDto : BaseDtos
{
    public int GrievanceCategoryId { get; set; }
    public string? GrievanceCategoryName { get; set; }
    public string Remark { get; set; } = null!;
    public string? Description { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetGrievanceRemarkDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetGrievanceRemark_GrievanceCategoryId_Required")]
    public int GrievanceCategoryId { get; set; }

    [Required(ErrorMessage = "AssetGrievanceRemark_Remark_Required")]
    [StringLength(150, ErrorMessage = "AssetGrievanceRemark_Remark_MaxLen_150")]
    public string Remark { get; set; } = null!;

    [StringLength(500, ErrorMessage = "AssetGrievanceRemark_Description_MaxLen_500")]
    public string? Description { get; set; }
}

public class UpdateAssetGrievanceRemarkDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetGrievanceRemark_GrievanceCategoryId_Required")]
    public int GrievanceCategoryId { get; set; }

    [Required(ErrorMessage = "AssetGrievanceRemark_Remark_Required")]
    [StringLength(150, ErrorMessage = "AssetGrievanceRemark_Remark_MaxLen_150")]
    public string Remark { get; set; } = null!;

    [StringLength(500, ErrorMessage = "AssetGrievanceRemark_Description_MaxLen_500")]
    public string? Description { get; set; }
}
