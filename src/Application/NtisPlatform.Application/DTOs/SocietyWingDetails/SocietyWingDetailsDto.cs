using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

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
    public int? WingPhoto { get; set; }
    public int? BoardPhoto { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateSocietyWingDetailsDto : CreateBaseDtos
{
    public int? WingId { get; set; }
    public int? PropertyId { get; set; }
    public int? SocietyDetailId { get; set; }

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
    public int? WingPhoto { get; set; }
    public int? BoardPhoto { get; set; }
}

public class UpdateSocietyWingDetailsDto : UpdateBaseDtos
{
    
    public int? WingId { get; set; }
    public int? PropertyId { get; set; }
    public int? SocietyDetailId { get; set; }

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

    public int? WingPhoto { get; set; }

    public int? BoardPhoto { get; set; }
}
