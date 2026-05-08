using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class SocietyDetailsDto : BaseDtos
{
    public int? PropertyId { get; set; }
    public int? WingId { get; set; }
    public string? WingName { get; set; }
    public string? SocietyName { get; set; }
    public string? SocietyAddress { get; set; }
    public string? SecretaryName { get; set; }
    public string? ManagerName { get; set; }
    public string? LandOwnerName { get; set; }
    public string? BuilderName { get; set; }
    public string? SecretaryNameEnglish { get; set; }
    public string? SocietyNameEnglish { get; set; }
    public string? SocietyAddressEnglish { get; set; }
    public string? ManagerNameEnglish { get; set; }
    public string? LandOwnerNameEnglish { get; set; }
    public string? BuilderNameEnglish { get; set; }
    public string? ManagerMobileNo { get; set; }
    public string? SecretaryMobileNo { get; set; }
    public string? SocietyEmailId { get; set; }
    public string? SecretaryEmailId { get; set; }
    public string? ManagerEmailId { get; set; }
    public bool MarkedForDeletion { get; set; }
}

public class CreateSocietyDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "SocietyDetails_PropertyId_Required")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "SocietyDetails_WingId_Required")]
    public int WingId { get; set; }

    [StringLength(30, ErrorMessage = "SocietyDetails_WingName_MaxLen_30")]
    public string? WingName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyDetails_SocietyName_MaxLen_500")]
    public string? SocietyName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SocietyAddress_MaxLen_200")]
    public string? SocietyAddress { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SecretaryName_MaxLen_200")]
    public string? SecretaryName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_ManagerName_MaxLen_200")]
    public string? ManagerName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_LandOwnerName_MaxLen_200")]
    public string? LandOwnerName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_BuilderName_MaxLen_200")]
    public string? BuilderName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SecretaryNameEnglish_MaxLen_200")]
    public string? SecretaryNameEnglish { get; set; }

    [StringLength(500, ErrorMessage = "SocietyDetails_SocietyNameEnglish_MaxLen_500")]
    public string? SocietyNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SocietyAddressEnglish_MaxLen_200")]
    public string? SocietyAddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_ManagerNameEnglish_MaxLen_200")]
    public string? ManagerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_LandOwnerNameEnglish_MaxLen_200")]
    public string? LandOwnerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_BuilderNameEnglish_MaxLen_200")]
    public string? BuilderNameEnglish { get; set; }

    [StringLength(13, ErrorMessage = "SocietyDetails_ManagerMobileNo_MaxLen_13")]
    public string? ManagerMobileNo { get; set; }

    [StringLength(13, ErrorMessage = "SocietyDetails_SecretaryMobileNo_MaxLen_13")]
    public string? SecretaryMobileNo { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_SocietyEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_SocietyEmailId_Invalid")]
    public string? SocietyEmailId { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_SecretaryEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_SecretaryEmailId_Invalid")]
    public string? SecretaryEmailId { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_ManagerEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_ManagerEmailId_Invalid")]
    public string? ManagerEmailId { get; set; }
}

public class UpdateSocietyDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "SocietyDetails_PropertyId_Required")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "SocietyDetails_WingId_Required")]
    public int WingId { get; set; }

    [StringLength(30, ErrorMessage = "SocietyDetails_WingName_MaxLen_30")]
    public string? WingName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyDetails_SocietyName_MaxLen_500")]
    public string? SocietyName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SocietyAddress_MaxLen_200")]
    public string? SocietyAddress { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SecretaryName_MaxLen_200")]
    public string? SecretaryName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_ManagerName_MaxLen_200")]
    public string? ManagerName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_LandOwnerName_MaxLen_200")]
    public string? LandOwnerName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_BuilderName_MaxLen_200")]
    public string? BuilderName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SecretaryNameEnglish_MaxLen_200")]
    public string? SecretaryNameEnglish { get; set; }

    [StringLength(500, ErrorMessage = "SocietyDetails_SocietyNameEnglish_MaxLen_500")]
    public string? SocietyNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_SocietyAddressEnglish_MaxLen_200")]
    public string? SocietyAddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_ManagerNameEnglish_MaxLen_200")]
    public string? ManagerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_LandOwnerNameEnglish_MaxLen_200")]
    public string? LandOwnerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyDetails_BuilderNameEnglish_MaxLen_200")]
    public string? BuilderNameEnglish { get; set; }

    [StringLength(13, ErrorMessage = "SocietyDetails_ManagerMobileNo_MaxLen_13")]
    public string? ManagerMobileNo { get; set; }

    [StringLength(13, ErrorMessage = "SocietyDetails_SecretaryMobileNo_MaxLen_13")]
    public string? SecretaryMobileNo { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_SocietyEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_SocietyEmailId_Invalid")]
    public string? SocietyEmailId { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_SecretaryEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_SecretaryEmailId_Invalid")]
    public string? SecretaryEmailId { get; set; }

    [StringLength(100, ErrorMessage = "SocietyDetails_ManagerEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "SocietyDetails_ManagerEmailId_Invalid")]
    public string? ManagerEmailId { get; set; }
}
