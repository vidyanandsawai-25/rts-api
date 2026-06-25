using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Master table defining the 9 property workflow stages: Geo-Sequencing through Bill Generation.
/// Maps to PTIS.PropertyWorkflowStageMaster table.
/// Provides metadata for stage display (name, order, description) and navigation to workflow details.
/// </summary>
[Table("PropertyWorkflowStageMaster", Schema = "PTIS")]
public class PropertyWorkflowStageMasterEntity
{
    /// <summary>
    /// Workflow stage identifier (1-9 representing the 9 stages).
    /// Primary key in PropertyWorkflowStageMaster table.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the workflow stage (e.g., "GeoSequencing", "InternalSurvey").
    /// Max length: 100 characters.
    /// </summary>
    public string StageName { get; set; } = null!;

    /// <summary>
    /// Optional detailed description of what happens in this stage.
    /// Max length: 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order (1-9) used for UI rendering of stage progress bars.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Soft delete flag: true means stage is active, false means inactive.
    /// Default: 1 (active).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this stage record.
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Date/time when this stage record was created.
    /// Default: GETDATE() at database level.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// User ID who last updated this stage record.
    /// </summary>
    public int? UpdatedBy { get; set; }

    /// <summary>
    /// Date/time when this stage record was last updated.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }
}
