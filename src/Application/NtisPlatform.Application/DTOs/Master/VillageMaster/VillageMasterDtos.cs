using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.VillageMaster;

public class VillageMasterDtos : BaseDtos
{
    public int ZoneId { get; set; }
    public string? VillageName { get; set; }
    public string? VillageNameEnglish { get; set; }
    public string? Pincode { get; set; }
}

public class CreateVillageMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ZoneId_Invalid")]
    public int ZoneId { get; set; }

    [Required(ErrorMessage = "VillageName_Required")]
    [StringLength(100, ErrorMessage = "VillageName_MaxLen_100")]
    [RegularExpression(@"^[^<>=]*$", ErrorMessage = "VillageName_InvalidCharacters")]
    public string? VillageName { get; set; }

    [Required(ErrorMessage = "VillageNameEnglish_Required")]
    [StringLength(100, ErrorMessage = "VillageNameEnglish_MaxLen_100")]
    [RegularExpression(@"^[A-Za-z\s.'-]+$",ErrorMessage = "VillageNameEnglish_OnlyEnglishCharacters")]
    public string? VillageNameEnglish { get; set; }

    [Required(ErrorMessage = "Pincode_Required")]
    [StringLength(20, ErrorMessage = "Pincode_MaxLen_20")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "Pincode_Invalid")]
    public string? Pincode { get; set; }
}

public class UpdateVillageMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ZoneId_Invalid")]
    public int ZoneId { get; set; }

    [Required(ErrorMessage = "VillageName_Required")]
    [StringLength(100, ErrorMessage = "VillageName_MaxLen_100")]
    [RegularExpression(@"^[^<>=]*$", ErrorMessage = "VillageName_InvalidCharacters")]
    public string? VillageName { get; set; }

    [Required(ErrorMessage = "VillageNameEnglish_Required")]
    [StringLength(100, ErrorMessage = "VillageNameEnglish_MaxLen_100")]
    [RegularExpression(@"^[A-Za-z\s.'-]+$", ErrorMessage = "VillageNameEnglish_OnlyEnglishCharacters")]
    public string? VillageNameEnglish { get; set; }

    [Required(ErrorMessage = "Pincode_Required")]
    [StringLength(20, ErrorMessage = "Pincode_MaxLen_20")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "Pincode_Invalid")]
    public string? Pincode { get; set; }
}
