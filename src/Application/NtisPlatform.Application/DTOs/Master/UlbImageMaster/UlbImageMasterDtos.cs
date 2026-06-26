using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class UlbImageMasterDto : BaseDtos
{
    public string? ImageType { get; set; }
    public int? ImageId { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateUlbImageMasterDto : CreateBaseDtos
{
    private string? _imageType;

    [Required(ErrorMessage = "UlbImageMaster_ImageType_Required")]
    [StringLength(50, ErrorMessage = "UlbImageMaster_ImageType_MaxLen_50")]
    public string? ImageType
    {
        get => _imageType;
        set => _imageType = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = "UlbImageMaster_ImageId_Invalid")]
    public int? ImageId { get; set; }
}

public class UpdateUlbImageMasterDto : UpdateBaseDtos
{
    private string? _imageType;

    [Required(ErrorMessage = "UlbImageMaster_ImageType_Required")]
    [StringLength(50, ErrorMessage = "UlbImageMaster_ImageType_MaxLen_50")]
    public string? ImageType
    {
        get => _imageType;
        set => _imageType = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = "UlbImageMaster_ImageId_Invalid")]
    public int? ImageId { get; set; }
}
