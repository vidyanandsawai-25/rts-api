using NtisPlatform.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.WingDetailsMast;

public class WingDetailsMastDto : BaseDtos
{
    public int SocietyDetailsMastId { get; set; }
    public int WingMasterId { get; set; }
    public string? WingName { get; set; }
    public string? SecretaryName { get; set; }
    public string? ManagerName { get; set; }
    public string? SecretaryNameEnglish { get; set; }
    public string? ManagerNameEnglish { get; set; }
    public string? ManagerMobileNo { get; set; }
    public int? ManagerMobileNoRemarkId { get; set; }
    public string? SecretaryMobileNo { get; set; }
    public int? SecretaryMobileNoRemarkId { get; set; }
    public string? SecretaryEmailId { get; set; }
    public string? ManagerEmailId { get; set; }

    //public string? WingPhotoDocumentIds { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    public SocietyWingDetailsDto? SocietyWingDetails { get; set; }
}

public class WingDetailsMastResponseDto
{
    public List<WingDetailsMastDto> WingDetailsMast { get; set; } = new();

    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public List<OldWingDetailsDto> OldWingDetails { get; set; } = new();
}

public class CreateWingDetailsMastDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WingDetailsMast_SocietyDetailsMastId_Required")]
    public int SocietyDetailsMastId { get; set; }

    [Required(ErrorMessage = "WingDetailsMast_WingMasterId_Required")]
    public int WingMasterId { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_WingName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_WingName_InvalidCharacters")]
    public string? WingName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_SecretaryName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_SecretaryName_InvalidCharacters")]
    public string? SecretaryName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_ManagerName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_ManagerName_InvalidCharacters")]
    public string? ManagerName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_SecretaryNameEnglish_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_SecretaryNameEnglish_InvalidCharacters")]
    public string? SecretaryNameEnglish { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_ManagerNameEnglish_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_ManagerNameEnglish_InvalidCharacters")]
    public string? ManagerNameEnglish { get; set; }

    [StringLength(15, ErrorMessage = "WingDetailsMast_ManagerMobileNo_MaxLen_15")]
    public string? ManagerMobileNo { get; set; }

    public int? ManagerMobileNoRemarkId { get; set; }

    [StringLength(15, ErrorMessage = "WingDetailsMast_SecretaryMobileNo_MaxLen_15")]
    public string? SecretaryMobileNo { get; set; }

    public int? SecretaryMobileNoRemarkId { get; set; }

    [StringLength(100, ErrorMessage = "WingDetailsMast_SecretaryEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "WingDetailsMast_SecretaryEmailId_InvalidFormat")]
    public string? SecretaryEmailId { get; set; }

    [StringLength(100, ErrorMessage = "WingDetailsMast_ManagerEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "WingDetailsMast_ManagerEmailId_InvalidFormat")]
    public string? ManagerEmailId { get; set; }

    [StringLength(1000, ErrorMessage = "WingDetailsMast_WingPhotoDocumentIds_MaxLen_1000")]
    public string? WingPhotoDocumentIds { get; set; }

    /// <summary>
    /// Stores additional wing details such as floor range and room counts.
    /// </summary>
    public CreateSocietyWingDetailsDto? SocietyWingDetails { get; set; }
}

public class UpdateWingDetailsMastDto : UpdateBaseDtos
{

    [StringLength(500, ErrorMessage = "WingDetailsMast_WingName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_WingName_InvalidCharacters")]
    public string? WingName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_SecretaryName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_SecretaryName_InvalidCharacters")]
    public string? SecretaryName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_ManagerName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_ManagerName_InvalidCharacters")]
    public string? ManagerName { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_SecretaryNameEnglish_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_SecretaryNameEnglish_InvalidCharacters")]
    public string? SecretaryNameEnglish { get; set; }

    [StringLength(500, ErrorMessage = "WingDetailsMast_ManagerNameEnglish_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "WingDetailsMast_ManagerNameEnglish_InvalidCharacters")]
    public string? ManagerNameEnglish { get; set; }

    [StringLength(15, ErrorMessage = "WingDetailsMast_ManagerMobileNo_MaxLen_15")]
    [RegularExpression(@"^[0-9]*$", ErrorMessage = "WingDetailsMast_ManagerMobileNo_OnlyDigits")]
    public string? ManagerMobileNo { get; set; }

    public int? ManagerMobileNoRemarkId { get; set; }

    [StringLength(15, ErrorMessage = "WingDetailsMast_SecretaryMobileNo_MaxLen_15")]
    [RegularExpression(@"^[0-9]*$", ErrorMessage = "WingDetailsMast_SecretaryMobileNo_OnlyDigits")]
    public string? SecretaryMobileNo { get; set; }

    public int? SecretaryMobileNoRemarkId { get; set; }

    [StringLength(100, ErrorMessage = "WingDetailsMast_SecretaryEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "WingDetailsMast_SecretaryEmailId_InvalidFormat")]
    public string? SecretaryEmailId { get; set; }

    [StringLength(100, ErrorMessage = "WingDetailsMast_ManagerEmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "WingDetailsMast_ManagerEmailId_InvalidFormat")]
    public string? ManagerEmailId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }

    public UpdateSocietyWingDetailsDto? SocietyWingDetails { get; set; }
}
