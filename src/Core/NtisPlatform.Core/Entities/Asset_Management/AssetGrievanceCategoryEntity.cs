using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management
{
    /// <summary>
    /// Entity representing an asset grievance category for complaint management under AMS.
    /// Maps to [AMS].[GrievanceCategoryMaster]
    /// </summary>
    public class AssetGrievanceCategoryEntity : BaseEntity, IHardDeletable
    {
        /// <summary>
        /// Display name of the grievance category (nvarchar(150))
        /// </summary>
        public string CategoryName { get; set; } = null!;

        /// <summary>
        /// Detailed description of the grievance category (nvarchar(500))
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Resolution SLA in days (int, NOT NULL, default 7)
        /// </summary>
        public int ResolutionSlaDays { get; set; } = 7;

        /// <summary>
        /// Flag indicating if entity is marked for soft/hard deletion
        /// </summary>
        public bool MarkedForDeletion { get; set; }

        /// <summary>
        /// Date when entity was marked for deletion
        /// </summary>
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
