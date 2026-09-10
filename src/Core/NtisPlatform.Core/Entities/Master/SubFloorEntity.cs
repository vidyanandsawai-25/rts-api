using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a sub floor entity manage sub floor information.
/// </summary>
public class SubFloorEntity :BaseEntity
{
    public string? SubFloorCode { get; set; }
    public string? Description { get; set; }
    public decimal? SubFloorPercentage { get; set; }

    /// <summary>
    /// Legacy sequence number (Column removed from PTIS.SubFloorMaster DB table, kept as [NotMapped] for backward compatibility)
    /// </summary>
    [NotMapped]
    public int? SequenceNo { get; set; }
    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();
}
