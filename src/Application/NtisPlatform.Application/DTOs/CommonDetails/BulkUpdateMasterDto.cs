using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateMasterDto : BaseDtos
{
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string? ReferenceTableName { get; set; }
}

public class CreateBulkUpdateMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "BulkUpdateMaster_UpdateCode_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateMaster_UpdateCode_MaxLen_50")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_UpdateName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_UpdateName_MaxLen_200")]
    public string UpdateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_ReferenceTableName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_ReferenceTableName_MaxLen_200")]
    public string ReferenceTableName { get; set; } = string.Empty;
}

public class UpdateBulkUpdateMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "BulkUpdateMaster_UpdateCode_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateMaster_UpdateCode_MaxLen_50")]
    [RegularExpression(@".*\S.*", ErrorMessage = "BulkUpdateMaster_UpdateCode_Required")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_UpdateName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_UpdateName_MaxLen_200")]
    public string UpdateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_ReferenceTableName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_ReferenceTableName_MaxLen_200")]
    public string ReferenceTableName { get; set; } = string.Empty;

    public bool? IsApprovalRequired { get; set; }
}
