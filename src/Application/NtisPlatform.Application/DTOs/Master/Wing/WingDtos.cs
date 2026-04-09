using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class WingDto : BaseDtos
{
    public int Id { get; set; }
    public string WingNo { get; set; } = string.Empty;

    public int? SequenceNo { get; set; }
}

public class CreateWingDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Wing_WingNo_Required")]
    [StringLength(10, ErrorMessage = "Wing_WingNo_MaxLen_10")]
    public string WingNo { get; set; } = string.Empty;

    [Range(1, 999, ErrorMessage = "Wing_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}

public class UpdateWingDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Wing_WingNo_Required")]
    [StringLength(10, ErrorMessage = "Wing_WingNo_MaxLen_10")]
    public string WingNo { get; set; } = string.Empty;

    [Range(1, 999, ErrorMessage = "Wing_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}