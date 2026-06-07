using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetOrganizationMasterDto : BaseDtos
{
    public int AuthorityId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public class CreateAssetOrganizationMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetOrganizationMaster_AuthorityId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetOrganizationMaster_AuthorityId_InvalidRange")]
    public int AuthorityId { get; set; }

    [Required(ErrorMessage = "AssetOrganizationMaster_OrganizationCode_Required")]
    [StringLength(20, ErrorMessage = "AssetOrganizationMaster_OrganizationCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetOrganizationMaster_OrganizationCode_Invalid")]
    public string OrganizationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetOrganizationMaster_OrganizationName_Required")]
    [StringLength(200, ErrorMessage = "AssetOrganizationMaster_OrganizationName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetOrganizationMaster_OrganizationName_Invalid")]
    public string OrganizationName { get; set; } = string.Empty;
}

public class UpdateAssetOrganizationMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetOrganizationMaster_AuthorityId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetOrganizationMaster_AuthorityId_InvalidRange")]
    public int AuthorityId { get; set; }

    [Required(ErrorMessage = "AssetOrganizationMaster_OrganizationCode_Required")]
    [StringLength(20, ErrorMessage = "AssetOrganizationMaster_OrganizationCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetOrganizationMaster_OrganizationCode_Invalid")]
    public string OrganizationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetOrganizationMaster_OrganizationName_Required")]
    [StringLength(200, ErrorMessage = "AssetOrganizationMaster_OrganizationName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetOrganizationMaster_OrganizationName_Invalid")]
    public string OrganizationName { get; set; } = string.Empty;
}
