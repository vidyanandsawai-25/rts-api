using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetApplicationTypeDto : BaseDtos
{
    public string ApplicationTypeCode { get; set; } = string.Empty;
    public string ApplicationTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateAssetApplicationTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ApplicationType_ApplicationTypeCode_Required")]
    [StringLength(20, ErrorMessage = "ApplicationType_ApplicationTypeCode_MaxLengthExceeded_20")]
    public string? ApplicationTypeCode { get; set; }

    [Required(ErrorMessage = "ApplicationType_ApplicationTypeName_Required")]
    [StringLength(100, ErrorMessage = "ApplicationType_ApplicationTypeName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-ऀ-ॿঀ-৿]*$", ErrorMessage = "ApplicationType_ApplicationTypeName_Invalid")]
    public string? ApplicationTypeName { get; set; }

    [StringLength(500, ErrorMessage = "ApplicationType_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "ApplicationType_DisplayOrder_Invalid")]
    public int DisplayOrder { get; set; }
}

public class UpdateAssetApplicationTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ApplicationType_ApplicationTypeCode_Required")]
    [StringLength(20, ErrorMessage = "ApplicationType_ApplicationTypeCode_MaxLengthExceeded_20")]
    public string? ApplicationTypeCode { get; set; }

    [Required(ErrorMessage = "ApplicationType_ApplicationTypeName_Required")]
    [StringLength(100, ErrorMessage = "ApplicationType_ApplicationTypeName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-ऀ-ॿঀ-৿]*$", ErrorMessage = "ApplicationType_ApplicationTypeName_Invalid")]
    public string? ApplicationTypeName { get; set; }

    [StringLength(500, ErrorMessage = "ApplicationType_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "ApplicationType_DisplayOrder_Invalid")]
    public int DisplayOrder { get; set; }
}
