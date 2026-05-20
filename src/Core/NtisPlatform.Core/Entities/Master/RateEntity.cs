using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

public class RateEntity : BaseEntity
{
    public int FloorId { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseGroupId { get; set; }
    public int RateSectionId { get; set; }
    public int TaxZoneId { get; set; }
    public string RateRemark { get; set; } = string.Empty;
    public int YearRangeRVId { get; set; }
    public decimal? RateSquareFeet { get; set; }
    public decimal? RateSquareMeter { get; set; }
    public virtual ConstructionTypeEntity? ConstructionType { get; set; }
    public virtual FloorEntity? Floor { get; set; }
    public virtual RateSectionEntity? RateSection { get; set; }
    public virtual TaxZoneEntity? TaxZone { get; set; }
    public virtual TypeOfUseGroupEntity? TypeOfUseGroup { get; set; }
    public virtual AssessmentYearRangeEntity? AssessmentYearRange { get; set; }
}

