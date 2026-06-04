using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateMasterDto : BaseDtos
{
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string UpdateNameMarathi { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string ReferenceTableName { get; set; } = string.Empty;
    public int DisplaySequence { get; set; }
    public string ApiRoute { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateBulkUpdateMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "BulkUpdateMaster_UpdateCode_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateMaster_UpdateCode_MaxLen_50")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_UpdateName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_UpdateName_MaxLen_200")]
    public string UpdateName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "BulkUpdateMaster_UpdateNameMarathi_MaxLen_200")]
    public string UpdateNameMarathi { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "BulkUpdateMaster_IconName_MaxLen_100")]
    public string IconName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_ReferenceTableName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_ReferenceTableName_MaxLen_200")]
    public string ReferenceTableName { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "BulkUpdateMaster_DisplaySequence_Range")]
    public int DisplaySequence { get; set; }

    [Required(ErrorMessage = "BulkUpdateMaster_ApiRoute_Required")]
    [StringLength(500, ErrorMessage = "BulkUpdateMaster_ApiRoute_MaxLen_500")]
    public string ApiRoute { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "BulkUpdateMaster_Description_MaxLen_1000")]
    public string? Description { get; set; }
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

    [StringLength(200, ErrorMessage = "BulkUpdateMaster_UpdateNameMarathi_MaxLen_200")]
    public string UpdateNameMarathi { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "BulkUpdateMaster_IconName_MaxLen_100")]
    public string IconName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateMaster_ReferenceTableName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateMaster_ReferenceTableName_MaxLen_200")]
    public string ReferenceTableName { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "BulkUpdateMaster_DisplaySequence_Range")]
    public int DisplaySequence { get; set; }

    [Required(ErrorMessage = "BulkUpdateMaster_ApiRoute_Required")]
    [StringLength(500, ErrorMessage = "BulkUpdateMaster_ApiRoute_MaxLen_500")]
    public string ApiRoute { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "BulkUpdateMaster_Description_MaxLen_1000")]
    public string? Description { get; set; }
}
