using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

[Table("PropertySurveyVisit", Schema = "GSMS")]
public class PropertySurveyVisitEntity : BaseEntity
{
    public int PropertyWorkflowDetailsId { get; set; }
    public bool? InternalSurveyVerified { get; set; }
    public int? RemarkId { get; set; }
    public string? RemarkText { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }

    [ForeignKey(nameof(PropertyWorkflowDetailsId))]
    public virtual PropertyWorkflowDetailsEntity PropertyWorkflowDetails { get; set; } = null!;

    [ForeignKey(nameof(RemarkId))]
    public virtual CommonRemarkDetailsEntity? Remark { get; set; }
}
