using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;

public class AgeFactorCVMasterDto : BaseDtos
{
    public int ConstructionTypeId { get; set; }
    public int AgeFrom { get; set; }
    public int AgeTo { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
    public string? ConstructionCode { get; set; }
    public string? ConstructionDescription { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
}

public class CreateAgeFactorCVMasterDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AgeFrom_Required")]
    [Range(0, 999, ErrorMessage = "AgeFrom_Range")]
    public int AgeFrom { get; set; }

    [Required(ErrorMessage = "AgeTo_Required")]
    [Range(0, 999, ErrorMessage = "AgeTo_Range")]
    public int AgeTo { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

public class UpdateAgeFactorCVMasterDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AgeFrom_Required")]
    [Range(0, 999, ErrorMessage = "AgeFrom_Range")]
    public int AgeFrom { get; set; }

    [Required(ErrorMessage = "AgeTo_Required")]
    [Range(0, 999, ErrorMessage = "AgeTo_Range")]
    public int AgeTo { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

// Bulk operation DTOs
public class BulkCreateAgeFactorCVMasterDto
{
    public List<CreateAgeFactorCVMasterDto> AgeFactors { get; set; } = new();
}

public class BulkUpdateAgeFactorCVMasterItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Id_Required")]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConstructionTypeId_Required")]
    public int? ConstructionTypeId { get; set; }

    [Range(0, 999, ErrorMessage = "AgeFrom_Range")]
    public int? AgeFrom { get; set; }

    [Range(0, 999, ErrorMessage = "AgeTo_Range")]
    public int? AgeTo { get; set; }

    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal? Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int? YearRangeCVId { get; set; }
}

public class BulkUpdateAgeFactorCVMasterDto
{
    public List<BulkUpdateAgeFactorCVMasterItemDto> AgeFactors { get; set; } = new();
}

public class BulkDeleteAgeFactorCVMasterDto
{
    public List<int> Ids { get; set; } = new();
}

public class BulkAgeFactorOperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AgeFactorCVMasterDto>? Items { get; set; }
    public List<BulkAgeFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkAgeFactorDeleteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int>? Items { get; set; }
    public List<BulkAgeFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkAgeFactorOperationErrorDto
{
    public int? Id { get; set; }
    public int? Index { get; set; }
    public string Message { get; set; } = string.Empty;
}
