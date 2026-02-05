using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RateMasterForCVDto : CommonBaseDtos
{
    public int Id { get; set; }

    public int MoujaId { get; set; }

    public string SubZoneNo { get; set; } = string.Empty;

    public string SubZoneName { get; set; } = string.Empty;

    public string CSN { get; set; } = string.Empty;

    public decimal? OpenPlotRate { get; set; }

    public decimal? ResidentialRate { get; set; }

    public decimal? OfficeRate { get; set; }

    public decimal? ShopRate { get; set; }

    public decimal? IndustrialRate { get; set; }
    
}

public class CreateRateMasterForCVDto : CreateCommonBaseDtos
{
    public int Id { get; set; }

    [Required(ErrorMessage = "CVRate_MoujaId_Required")]
    public int MoujaId { get; set; }

    [StringLength(20, ErrorMessage = "CVRate_SubZoneNo_MaxLen_20")]
    [Required(ErrorMessage = "CVRate_SubZoneNo_Required")]
    public string SubZoneNo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "CVRate_SubZoneName_MaxLen_1000")]
    [Required(ErrorMessage = "CVRate_SubZoneName_Required")]
    public string SubZoneName { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "CVRate_CSN_MaxLen_4000")]
    [Required(ErrorMessage = "CVRate_CSN_Required")]
    public string CSN { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_OpenPlotRate_Min_0")]
    public decimal? OpenPlotRate { get; set; }
    
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_ResidentialRate_Min_0")]
    public decimal? ResidentialRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_OfficeRate_Min_0")]
    public decimal? OfficeRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_ShopRate_Min_0")]
    public decimal? ShopRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_IndustrialRate_Min_0")]
    public decimal? IndustrialRate { get; set; }
    
}


public class UpdateRateMasterForCVDto : UpdateCommonBaseDtos
{    
    public int Id { get; set; }

    [Required(ErrorMessage = "CVRate_MoujaId_Required")]
    public int MoujaId { get; set; }

    [StringLength(20, ErrorMessage = "CVRate_SubZoneNo_MaxLen_20")]
    [Required(ErrorMessage = "CVRate_SubZoneNo_Required")]
    public string SubZoneNo { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "CVRate_SubZoneName_MaxLen_1000")]
    [Required(ErrorMessage = "CVRate_SubZoneName_Required")]
    public string SubZoneName { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "CVRate_CSN_MaxLen_4000")]
    [Required(ErrorMessage = "CVRate_CSN_Required")]
    public string CSN { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_OpenPlotRate_Min_0")]
    public decimal? OpenPlotRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_ResidentialRate_Min_0")]
    public decimal? ResidentialRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_OfficeRate_Min_0")]
    public decimal? OfficeRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_ShopRate_Min_0")]
    public decimal? ShopRate { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_IndustrialRate_Min_0")]
    public decimal? IndustrialRate { get; set; }

}
