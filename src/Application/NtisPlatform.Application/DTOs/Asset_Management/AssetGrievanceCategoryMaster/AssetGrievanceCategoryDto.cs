using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetGrievanceCategoryDto : BaseDtos
{
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public int ResolutionSlaDays { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetGrievanceCategoryDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetGrievanceCategory_CategoryName_Required")]
    [StringLength(150, ErrorMessage = "AssetGrievanceCategory_CategoryName_MaxLen_150")]
    public string CategoryName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "AssetGrievanceCategory_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetGrievanceCategory_ResolutionSlaDays_MustBeNonNegative")]
    public int ResolutionSlaDays { get; set; } = 7;
}

public class UpdateAssetGrievanceCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetGrievanceCategory_CategoryName_Required")]
    [StringLength(150, ErrorMessage = "AssetGrievanceCategory_CategoryName_MaxLen_150")]
    public string CategoryName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "AssetGrievanceCategory_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetGrievanceCategory_ResolutionSlaDays_MustBeNonNegative")]
    public int ResolutionSlaDays { get; set; } = 7;
}
