using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property assessment data in the PTIS system (PropertyMastDetails table)
/// </summary>
[Table("PropertyMastDetails", Schema = "PTIS")]
public class PropertyAssessmentEntity : BaseEntity
{
    [Key]
    public int PropertyDetailsId { get; set; }

    public int PropertyId { get; set; }

    public int? WingId { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? WingNo { get; set; }

    public int? NoOfResidentialToilets { get; set; }

    public int? NoOfCommercialToilets { get; set; }
}
