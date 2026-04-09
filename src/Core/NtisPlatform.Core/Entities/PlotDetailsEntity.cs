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
    public int Id { get; set; }

    public int? PropertyId { get; set; }

    [Column(TypeName = "float")]
    public double? PlotArea { get; set; }

    [Column(TypeName = "float")]
    public double? PlotTaxableAreaSqFt { get; set; }

    [MaxLength(10)]
    public string? OpenPlotType { get; set; }

    [MaxLength(1000)]
    public string? OpenPlotRenterName { get; set; }

    [Column(TypeName = "float")]
    public double? OpenPlotLength { get; set; }

    [Column(TypeName = "float")]
    public double? OpenPlotWidth { get; set; }

    [Column(TypeName = "float")]
    public double? PlotTaxableAreaSqMtr { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaSqMtr { get; set; }

    [Column(TypeName = "varchar(30)")]
    public string? OpenPlotSubmissionType { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaMtrLength { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaMtrWidth { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaFtLength { get; set; }

    [Column(TypeName = "float")]
    public double? PlotAreaFtWidth { get; set; }

    public bool MarkedForDeletion { get; set; } = false;
}