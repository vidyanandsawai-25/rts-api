using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.BlockMaster;

public class BlockMasterDtos : BaseDtos
{
    public int WardId { get; set; }
    public string BlockNo { get; set; } = string.Empty;
}

public class CreateBlockMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardId_Invalid")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "BlockNo_Required")]
    [StringLength(20, ErrorMessage = "BlockNo_MaxLen_20")]
    public string BlockNo { get; set; } = string.Empty;
}

public class UpdateBlockMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardId_Invalid")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "BlockNo_Required")]
    [StringLength(20, ErrorMessage = "BlockNo_MaxLen_20")]
    public string BlockNo { get; set; } = string.Empty;
}