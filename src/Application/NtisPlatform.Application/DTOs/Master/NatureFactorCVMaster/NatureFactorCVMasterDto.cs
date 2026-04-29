using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;

public class NatureFactorCVMasterDto : BaseDtos
{
    public int ConstructionTypeId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
    public string? ConstructionCode { get; set; }
    public string? ConstructionDescription { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
}

public class CreateNatureFactorCVMasterDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }

}

public class UpdateNatureFactorCVMasterDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }

}

// Bulk operation DTOs
public class BulkCreateNatureFactorCVMasterDto
{
    public List<CreateNatureFactorCVMasterDto> NatureFactors { get; set; } = new();
}

public class BulkUpdateNatureFactorCVMasterItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Id_Required")]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int? ConstructionTypeId { get; set; }

    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal? Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int? YearRangeCVId { get; set; }
}

public class BulkUpdateNatureFactorCVMasterDto
{
    public List<BulkUpdateNatureFactorCVMasterItemDto> NatureFactors { get; set; } = new();
}

public class BulkDeleteNatureFactorCVMasterDto
{
    public List<int> Ids { get; set; } = new();
}

public class BulkNatureFactorOperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<NatureFactorCVMasterDto>? Items { get; set; }
    public List<BulkNatureFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkNatureFactorDeleteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int>? Items { get; set; }
    public List<BulkNatureFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkNatureFactorOperationErrorDto
{
    public int? Id { get; set; }
    public int? Index { get; set; }
    public string Message { get; set; } = string.Empty;
}
