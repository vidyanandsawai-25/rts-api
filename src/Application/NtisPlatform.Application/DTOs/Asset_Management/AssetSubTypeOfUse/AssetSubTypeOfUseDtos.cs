using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetSubTypeOfUseDto : BaseDtos
{
    public string? Description { get; set; }
    public int TypeOfUseId { get; set; }
    public int? SearchSequence { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetSubTypeOfUseDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetSubTypeOfUse_Description_Required")]
    [StringLength(100, ErrorMessage = "AssetSubTypeOfUse_Description_MaxLength")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "AssetSubTypeOfUse_TypeOfUseId_Required")]
    public int TypeOfUseId { get; set; }

    public int? SearchSequence { get; set; }
}

public class UpdateAssetSubTypeOfUseDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetSubTypeOfUse_Description_Required")]
    [StringLength(100, ErrorMessage = "AssetSubTypeOfUse_Description_MaxLength")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "AssetSubTypeOfUse_TypeOfUseId_Required")]
    public int TypeOfUseId { get; set; }

    public int? SearchSequence { get; set; }
}
