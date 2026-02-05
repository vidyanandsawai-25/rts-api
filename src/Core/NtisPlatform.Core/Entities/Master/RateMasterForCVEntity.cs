using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;


public class RateMasterForCVEntity : CommonBaseEntity
{
    public int Id { get; set; } = 0;

    public int MoujaId { get; set; } 

    [MaxLength(20)]
    public string SubZoneNo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string SubZoneName { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string CSN { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    public decimal? OpenPlotRate { get; set; }

    [Column(TypeName = "money")]
    public decimal? ResidentialRate { get; set; }

    [Column(TypeName = "money")]
    public decimal? OfficeRate { get; set; }

    [Column(TypeName = "money")]
    public decimal? ShopRate { get; set; }

    [Column(TypeName = "money")]
    public decimal? IndustrialRate { get; set; }

}