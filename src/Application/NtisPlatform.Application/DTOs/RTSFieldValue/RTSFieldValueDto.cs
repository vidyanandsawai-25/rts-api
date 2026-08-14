using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSFieldValue;

public class RTSFieldValueDto : BaseDtos
{
    public int ApplicationId { get; set; }
    public int FieldDefinitionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public Guid? DocumentGuid { get; set; }
}

public class CreateRTSFieldValueDto : CreateBaseDtos
{
    //[Required(ErrorMessage = "RTSFieldValue_FieldDefinitionId_Required")]
    //[Range(1, int.MaxValue, ErrorMessage = "RTSFieldValue_FieldDefinitionId_InvalidRange")]
    public int FieldDefinitionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public Guid? DocumentGuid { get; set; }
}

public class UpdateRTSFieldValueDto : UpdateBaseDtos
{

    [Required(ErrorMessage = "RTSFieldValue_FieldDefinitionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldValue_FieldDefinitionId_InvalidRange")]
    public int FieldDefinitionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public Guid? DocumentGuid { get; set; }
}
