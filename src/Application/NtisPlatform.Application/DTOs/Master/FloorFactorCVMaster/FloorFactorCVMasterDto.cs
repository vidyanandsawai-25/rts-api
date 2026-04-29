using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;

public class FloorFactorCVMasterDto : BaseDtos
{
    public int FloorId { get; set; }
    public decimal FactorWithLift { get; set; }
    public decimal FactorWithoutLift { get; set; }
    public int YearRangeCVId { get; set; }
    public string? FloorCode { get; set; }
    public string? FloorDescription { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public bool HasFactorData => Id > 0;
}

public class CreateFloorFactorCVMasterDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "FloorId_Required")]
    public int FloorId { get; set; }

    [Range(0, 999.99, ErrorMessage = "FactorWithLift_Range")]
    public decimal FactorWithLift { get; set; }

    [Range(0, 999.99, ErrorMessage = "FactorWithoutLift_Range")]
    public decimal FactorWithoutLift { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

public class UpdateFloorFactorCVMasterDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "FloorId_Required")]
    public int FloorId { get; set; }

    [Range(0, 999.99, ErrorMessage = "FactorWithLift_Range")]
    public decimal FactorWithLift { get; set; }

    [Range(0, 999.99, ErrorMessage = "FactorWithoutLift_Range")]
    public decimal FactorWithoutLift { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

