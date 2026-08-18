using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class OldWardMasterDto : BaseDtos
{
    public string? OldZoneName { get; set; }
    public string? OldWardNo { get; set; }
}

public class CreateOldWardMasterDto : CreateBaseDtos
{
    [StringLength(100, ErrorMessage = "OldWardMaster_OldZoneName_MaxLen_100")]
    public string? OldZoneName { get; set; }

    [StringLength(50, ErrorMessage = "OldWardMaster_OldWardNo_MaxLen_50")]
    public string? OldWardNo { get; set; }
}

public class UpdateOldWardMasterDto : UpdateBaseDtos
{
    [StringLength(100, ErrorMessage = "OldWardMaster_OldZoneName_MaxLen_100")]
    public string? OldZoneName { get; set; }

    [StringLength(50, ErrorMessage = "OldWardMaster_OldWardNo_MaxLen_50")]
    public string? OldWardNo { get; set; }
}
