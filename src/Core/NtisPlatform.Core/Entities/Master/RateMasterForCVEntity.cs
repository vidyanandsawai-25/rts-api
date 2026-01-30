using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;


public class RateMasterForCVEntity : CommonBaseEntity
{
    public int ID { get; set; } = 0;

    // FK to MoujaMaster
    public int? MoujaId { get; set; } 

    [MaxLength(20)]
    public string? SubZoneNo { get; set; }

    [MaxLength(1000)]
    public string? SubZoneName { get; set; }

    [MaxLength(4000)]
    public string? CSN { get; set; }

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