using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetAuthorityMasterDto : BaseDtos
{
    public string AuthorityCode { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string? State { get; set; }
}

public class CreateAssetAuthorityMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetAuthorityMaster_AuthorityCode_Required")]
    [StringLength(20, ErrorMessage = "AssetAuthorityMaster_AuthorityCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_AuthorityCode_Invalid")]
    public string AuthorityCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetAuthorityMaster_AuthorityName_Required")]
    [StringLength(200, ErrorMessage = "AssetAuthorityMaster_AuthorityName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_AuthorityName_Invalid")]
    public string AuthorityName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetAuthorityMaster_State_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_State_Invalid")]
    public string? State { get; set; }
}

public class UpdateAssetAuthorityMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetAuthorityMaster_AuthorityCode_Required")]
    [StringLength(20, ErrorMessage = "AssetAuthorityMaster_AuthorityCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_AuthorityCode_Invalid")]
    public string AuthorityCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetAuthorityMaster_AuthorityName_Required")]
    [StringLength(200, ErrorMessage = "AssetAuthorityMaster_AuthorityName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_AuthorityName_Invalid")]
    public string AuthorityName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetAuthorityMaster_State_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetAuthorityMaster_State_Invalid")]
    public string? State { get; set; }
}
