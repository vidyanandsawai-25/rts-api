using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents plot details in the PTIS system
/// </summary>
public class PlotDetailsEntity : BaseEntity, IHardDeletable
{
   public int? PropertyId { get; set; }

    public double? PlotArea { get; set; }

    public double? PlotTaxableAreaSqFt { get; set; }

    public string? OpenPlotType { get; set; }

    public string? OpenPlotRenterName { get; set; }

    public double? OpenPlotLength { get; set; }

    public double? OpenPlotWidth { get; set; }

    public double? PlotTaxableAreaSqMtr { get; set; }

    public double? PlotAreaSqMtr { get; set; }

    public string? OpenPlotSubmissionType { get; set; }
    public double? PlotAreaMtrLength { get; set; }

    public double? PlotAreaMtrWidth { get; set; }

    public double? PlotAreaFtLength { get; set; }

    public double? PlotAreaFtWidth { get; set; }

    // Navigation property
    public virtual PropertyEntity? PropertyMast { get; set; }
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }
}