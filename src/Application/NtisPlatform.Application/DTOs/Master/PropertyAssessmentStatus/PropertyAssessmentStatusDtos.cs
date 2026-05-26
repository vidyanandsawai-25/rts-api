using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;

public class PropertyAssessmentStatusDto : BaseDtos
{
    public string StatusName { get; set; } = string.Empty;
}

public class CreatePropertyAssessmentStatusDto : CreateBaseDtos
{
    private string _statusName = string.Empty;

    [Required(ErrorMessage = "StatusName_Required")]
    [StringLength(30, ErrorMessage = "StatusName_MaxLen_30")]
    public string StatusName
    {
        get => _statusName;
        set => _statusName = value?.Trim() ?? string.Empty;
    }
}

public class UpdatePropertyAssessmentStatusDto : UpdateBaseDtos
{
    private string _statusName = string.Empty;

    [Required(ErrorMessage = "StatusName_Required")]
    [StringLength(30, ErrorMessage = "StatusName_MaxLen_30")]
    public string StatusName
    {
        get => _statusName;
        set => _statusName = value?.Trim() ?? string.Empty;
    }
}
