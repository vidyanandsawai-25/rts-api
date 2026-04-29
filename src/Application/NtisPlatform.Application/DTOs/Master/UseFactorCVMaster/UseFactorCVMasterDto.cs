using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;

public class UseFactorCVMasterDto : BaseDtos
{ 
    public int TypeOfUseId { get; set; }
    public int SubTypeOfUseId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? TypeOfUseDescription { get; set; }
    public string? Type { get; set; }
    public int? TypeOfUseGroupId { get; set; }
    public string? SubTypeOfUseDescription { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
}

public class CreateUseFactorCVMasterDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "TypeOfUseId_Required")]
    public int TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SubTypeOfUseId_Required")]
    public int SubTypeOfUseId { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }
}

public class UpdateUseFactorCVMasterDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "TypeOfUseId_Required")]
    public int TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SubTypeOfUseId_Required")]
    public int SubTypeOfUseId { get; set; }

    [Required(ErrorMessage = "Factor_Required")]
    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int YearRangeCVId { get; set; }

}

// Bulk operation DTOs
public class BulkCreateUseFactorCVMasterDto
{
    public List<CreateUseFactorCVMasterDto> UseFactors { get; set; } = new();
}

public class BulkUpdateUseFactorCVMasterItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Id_Required")]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TypeOfUseId_Required")]
    public int? TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SubTypeOfUseId_Required")]
    public int? SubTypeOfUseId { get; set; }

    [Range(0, 999.99, ErrorMessage = "Factor_Range")]
    public decimal? Factor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "YearRangeCVId_Required")]
    public int? YearRangeCVId { get; set; }
}

public class BulkUpdateUseFactorCVMasterDto
{
    public List<BulkUpdateUseFactorCVMasterItemDto> UseFactors { get; set; } = new();
}

public class BulkDeleteUseFactorCVMasterDto
{
    public List<int> Ids { get; set; } = new();
}

public class BulkUseFactorOperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<UseFactorCVMasterDto>? Items { get; set; }
    public List<BulkUseFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkUseFactorDeleteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<int>? Items { get; set; }
    public List<BulkUseFactorOperationErrorDto>? Errors { get; set; }
}

public class BulkUseFactorOperationErrorDto
{
    public int? Id { get; set; }
    public int? Index { get; set; }
    public string Message { get; set; } = string.Empty;
}
