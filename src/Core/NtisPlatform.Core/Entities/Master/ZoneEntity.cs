using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a zone master entities.
/// </summary>
public class ZoneEntity : BaseEntity
{
    public string ZoneNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SequenceNo { get; set; }
}

