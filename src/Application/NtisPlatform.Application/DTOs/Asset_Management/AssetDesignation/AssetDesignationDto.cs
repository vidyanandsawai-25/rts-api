using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetDesignationDto : BaseDtos
{
    public int OwningDepartmentId { get; set; }
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;
    public string? DesignationLocal { get; set; }
    public string? DesignationDescription { get; set; }

    /// <summary>[AMS].[OwningDepartmentMaster].OwningDepartmentName for <see cref="OwningDepartmentId"/>, resolved via join.</summary>
    public string? OwningDepartmentName { get; set; }
}

public class CreateAssetDesignationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Designation_OwningDepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Designation_OwningDepartmentId_Invalid")]
    public int? OwningDepartmentId { get; set; }

    [Required(ErrorMessage = "Designation_DesignationCode_Required")]
    [StringLength(50, ErrorMessage = "Designation_DesignationCode_MaxLengthExceeded_50")]
    public string? DesignationCode { get; set; }

    [Required(ErrorMessage = "Designation_DesignationName_Required")]
    [StringLength(100, ErrorMessage = "Designation_DesignationName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-ऀ-ॿঀ-৿]*$", ErrorMessage = "Designation_DesignationName_Invalid")]
    public string? DesignationName { get; set; }

    [StringLength(150, ErrorMessage = "Designation_DesignationLocal_MaxLengthExceeded_150")]
    public string? DesignationLocal { get; set; }

    [StringLength(250, ErrorMessage = "Designation_DesignationDescription_MaxLengthExceeded_250")]
    public string? DesignationDescription { get; set; }
}

public class UpdateAssetDesignationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Designation_OwningDepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Designation_OwningDepartmentId_Invalid")]
    public int? OwningDepartmentId { get; set; }

    [Required(ErrorMessage = "Designation_DesignationCode_Required")]
    [StringLength(50, ErrorMessage = "Designation_DesignationCode_MaxLengthExceeded_50")]
    public string? DesignationCode { get; set; }

    [Required(ErrorMessage = "Designation_DesignationName_Required")]
    [StringLength(100, ErrorMessage = "Designation_DesignationName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-ऀ-ॿঀ-৿]*$", ErrorMessage = "Designation_DesignationName_Invalid")]
    public string? DesignationName { get; set; }

    [StringLength(150, ErrorMessage = "Designation_DesignationLocal_MaxLengthExceeded_150")]
    public string? DesignationLocal { get; set; }

    [StringLength(250, ErrorMessage = "Designation_DesignationDescription_MaxLengthExceeded_250")]
    public string? DesignationDescription { get; set; }
}
