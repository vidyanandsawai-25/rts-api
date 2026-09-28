using NtisPlatform.Application.DTOs.PropertyPhoto;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class SocietyWingDetailsResponseDto
{
    public List<SocietyWingDetailsDto> WingSocietyDetails { get; set; } = new();

    public List<OldWingDetailsDto> OldWingDetails { get; set; } = new();
}

public class OldWingDetailsDto
{

    public string? OldSocietyName { get; set; }
    public string? OldWardNo { get; set; }
    public int? OldFloorCount { get; set; }
    public string? OldAddress { get; set; }
    public int? FlatShopCount { get; set; }
    public List<string> OldWingNo { get; set; } = new();

}

public class SocietyWingDetailsDto : BaseDtos
{
    public int? WingId { get; set; }
    public int? PropertyId { get; set; }
    public int? SocietyDetailId { get; set; }
    public string? FromFloor { get; set; }
    public string? ToFloor { get; set; }
    public string? OldWingName { get; set; }
    public string? NewWingName { get; set; }
    public int? NoOfFlat { get; set; }
    public int? NoOfShop { get; set; }
    public int? NoOfRowHouse { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public List<PropertyPhotoUploadResponseDto> WingPhotos { get; set; } = new();
    public List<PropertyPhotoUploadResponseDto> BoardPhotos { get; set; } = new();
}

public class CreateSocietyWingDetailsDto : CreateBaseDtos
{
    public int? WingId { get; set; }

    public int? PropertyId { get; set; }
    public int? SocietyDetailId { get; set; }
    public int? WingDetailsMastId { get; set; }

    [StringLength(50, ErrorMessage = "SocietyWingDetails_FromFloor_MaxLen_50")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_FromFloor_InvalidCharacters")]
    public string? FromFloor { get; set; }

    [StringLength(50, ErrorMessage = "SocietyWingDetails_ToFloor_MaxLen_50")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_ToFloor_InvalidCharacters")]
    public string? ToFloor { get; set; }

    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_OldWingName_InvalidCharacters")]
    public string? OldWingName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyWingDetails_NewWingName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_NewWingName_InvalidCharacters")]
    public string? NewWingName { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfFlat_NonNegative")]
    public int? NoOfFlat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfShop_NonNegative")]
    public int? NoOfShop { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfRowHouse_NonNegative")]
    public int? NoOfRowHouse { get; set; }

}

public class UpdateSocietyWingDetailsDto : UpdateBaseDtos
{

    [StringLength(50, ErrorMessage = "SocietyWingDetails_FromFloor_MaxLen_50")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_FromFloor_InvalidCharacters")]
    public string? FromFloor { get; set; }

    [StringLength(50, ErrorMessage = "SocietyWingDetails_ToFloor_MaxLen_50")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_ToFloor_InvalidCharacters")]
    public string? ToFloor { get; set; }

    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_OldWingName_InvalidCharacters")]
    public string? OldWingName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyWingDetails_NewWingName_MaxLen_500")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "SocietyWingDetails_NewWingName_InvalidCharacters")]
    public string? NewWingName { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfFlat_NonNegative")]
    public int? NoOfFlat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfShop_NonNegative")]
    public int? NoOfShop { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "SocietyWingDetails_NoOfRowHouse_NonNegative")]
    public int? NoOfRowHouse { get; set; }

}
