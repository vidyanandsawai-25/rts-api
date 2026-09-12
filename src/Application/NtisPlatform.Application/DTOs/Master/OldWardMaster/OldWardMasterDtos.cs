using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class OldWardMasterDto : BaseDtos
{
    public string OldWardNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OldZoneId { get; set; }
    public int? SequenceNo { get; set; }
}

public class CreateOldWardMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OldWardMaster_OldWardNo_Required")]
    [StringLength(20, ErrorMessage = "OldWardMaster_OldWardNo_MaxLen_20")]
    public string OldWardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OldWardMaster_Description_Required")]
    [StringLength(100, ErrorMessage = "OldWardMaster_Description_MaxLen_100")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "OldWardMaster_OldZoneId_Required")]
    public int OldZoneId { get; set; }

    public int? SequenceNo { get; set; }
}

public class UpdateOldWardMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OldWardMaster_OldWardNo_Required")]
    [StringLength(20, ErrorMessage = "OldWardMaster_OldWardNo_MaxLen_20")]
    public string OldWardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OldWardMaster_Description_Required")]
    [StringLength(100, ErrorMessage = "OldWardMaster_Description_MaxLen_100")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "OldWardMaster_OldZoneId_Required")]
    public int OldZoneId { get; set; }

    public int? SequenceNo { get; set; }
}

