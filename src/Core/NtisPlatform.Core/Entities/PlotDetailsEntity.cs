using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents plot details in the PTIS system
/// </summary>
[Table("PlotDetails", Schema = "PTIS")]
public class PlotDetailsEntity : BaseEntity
{
    [Key]
    public int PlotId { get; set; }

    public int PropertyId { get; set; }

    [Column(TypeName = "float")]
    public double? PlotArea { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaFtLength { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaFtWidth { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaMtrLength { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaMtrWidth { get; set; }
}
