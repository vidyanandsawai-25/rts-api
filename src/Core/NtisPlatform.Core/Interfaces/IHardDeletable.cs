namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Marker interface for entities that support hard deletion via background task.
/// When MarkedForDeletion is true, the entity will be soft-deleted immediately 
/// and permanently removed from the database during the nightly cleanup task.
/// </summary>
public interface IHardDeletable
{
    /// <summary>
    /// Indicates whether the entity is marked for permanent deletion.
    /// When set to true, entity will be soft-deleted and removed by the cleanup task.
    /// </summary>
    bool MarkedForDeletion { get; set; }
    
    /// <summary>
    /// Date and time when the entity was marked for deletion.
    /// Used by the cleanup task to determine when to perform hard deletion.
    /// </summary>
    DateTime? MarkedForDeletionDate { get; set; }
}
