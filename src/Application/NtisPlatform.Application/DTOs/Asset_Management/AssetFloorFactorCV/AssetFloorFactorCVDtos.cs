using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetFloorFactorCVDto : BaseDtos
{
    public int FloorId { get; set; }
    public decimal FactorWithLift { get; set; }
    public decimal FactorWithoutLift { get; set; }
    public int YearRangeCVId { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetFloorFactorCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetFloorFactorCV_FloorId_Required")]
    public int FloorId { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_FactorWithLift_Required")]
    [Range(0, 999.99, ErrorMessage = "AssetFloorFactorCV_FactorWithLift_Range")]
    public decimal FactorWithLift { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_FactorWithoutLift_Required")]
    [Range(0, 999.99, ErrorMessage = "AssetFloorFactorCV_FactorWithoutLift_Range")]
    public decimal FactorWithoutLift { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

public class UpdateAssetFloorFactorCVDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetFloorFactorCV_FloorId_Required")]
    public int FloorId { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_FactorWithLift_Required")]
    [Range(0, 999.99, ErrorMessage = "AssetFloorFactorCV_FactorWithLift_Range")]
    public decimal FactorWithLift { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_FactorWithoutLift_Required")]
    [Range(0, 999.99, ErrorMessage = "AssetFloorFactorCV_FactorWithoutLift_Range")]
    public decimal FactorWithoutLift { get; set; }

    [Required(ErrorMessage = "AssetFloorFactorCV_YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}
