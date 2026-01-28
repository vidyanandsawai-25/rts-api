using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

public class RateEntity : CommonBaseEntity
{
    public int ID { get; set; } = 0;

    [StringLength(5, ErrorMessage = "FloorID must be at most 5 characters")]
    public string? FloorID { get; set; }

    [StringLength(7, ErrorMessage = "ConstructionID must be at most 7 characters")]
    public string? ConstructionID { get; set; }

    public string? TypeOfUseGroupID { get; set; }

    public string? RateSectionNo { get; set; }

    [StringLength(10, ErrorMessage = "TaxZoneNo must be at most 10 characters")]
    public string? TaxZoneNo { get; set; }

    [StringLength(20, ErrorMessage = "RateRemark must be at most 20 characters")]
    public string? RateRemark { get; set; }

    [Range(1, 9999, ErrorMessage = "Year must be between 1 and 9999")]
    public int? Year { get; set; }

    [Range(1, 9999, ErrorMessage = "MinYear must be between 1 and 9999")]
    public int? MinYear { get; set; }

    [Range(1, 9999, ErrorMessage = "MaxYear must be between 1 and 9999")]
    public int? MaxYear { get; set; }

    [Column(TypeName = "money")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "RateSquareFeet must be >= 0")]
    public decimal? RateSquareFeet { get; set; }

    [Column(TypeName = "money")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "RateSquareMeter must be >= 0")]
    public decimal? RateSquareMeter { get; set; }

}

