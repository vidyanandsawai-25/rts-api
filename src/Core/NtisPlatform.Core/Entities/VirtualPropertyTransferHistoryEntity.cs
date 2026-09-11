namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents the history of virtual property transfers, including
/// source property details and transferred ward details.
/// </summary>
public class VirtualPropertyTransferHistoryEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets the original property identifier.
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// Gets or sets the ward identifier of the original property.
    /// </summary>
    public int WardId { get; set; }

    /// <summary>
    /// Gets or sets the original property number.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Gets or sets the partition number of the original property.
    /// </summary>
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Gets or sets the ward identifier of the transferred property.
    /// </summary>
    public int TransferredWardId { get; set; }

    /// <summary>
    /// Gets or sets the property number in the transferred ward.
    /// </summary>
    public string? TransferredPropertyNo { get; set; }

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the original property.
    /// </summary>
    public virtual PropertyEntity? Property { get; set; }

    /// <summary>
    /// Gets or sets the ward of the original property.
    /// </summary>
    public virtual WardEntity? Ward { get; set; }

    /// <summary>
    /// Gets or sets the ward of the transferred property.
    /// </summary>
    public virtual WardEntity? TransferredWard { get; set; }

    #endregion
}
