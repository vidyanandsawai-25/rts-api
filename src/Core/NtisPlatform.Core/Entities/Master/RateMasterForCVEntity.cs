using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;




public class RateMasterForCVEntity : BaseEntity
{


    public int SubZoneId { get; set; }

    public int? TypeOfUseGroupId { get; set; }

    public int? FloorGroupId { get; set; }

    public decimal RateAmount { get; set; }

    public int AssessmentYearRangeId { get; set; }
 
    public virtual AssessmentYearRangeCVEntity? AssessmentYearRange { get; set; }

    public virtual FloorGroupMasterEntity? FloorGroup { get; set; }

    public virtual TypeOfUseGroupEntity? TypeOfUseGroup { get; set; }



}