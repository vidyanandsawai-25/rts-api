using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management
{
    /// <summary>
    /// Entity representing an asset grievance remark master record under AMS.
    /// Maps to [AMS].[GrievanceRemarkMaster]
    /// </summary>
    public class AssetGrievanceRemarkMasterEntity : BaseEntity, IHardDeletable
    {
        /// <summary>
        /// Foreign key to Grievance Category
        /// </summary>
        public int GrievanceCategoryId { get; set; }

        /// <summary>
        /// Remark title/text (nvarchar(150))
        /// </summary>
        public string Remark { get; set; } = null!;

        /// <summary>
        /// Detailed description of the grievance remark (nvarchar(500))
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Flag indicating if entity is marked for soft/hard deletion
        /// </summary>
        public bool MarkedForDeletion { get; set; }

        /// <summary>
        /// Date when entity was marked for deletion
        /// </summary>
        public DateTime? MarkedForDeletionDate { get; set; }

        /// <summary>
        /// Associated GrievanceCategory navigation entity
        /// </summary>
        public virtual AssetGrievanceCategoryEntity? GrievanceCategory { get; set; }
    }
}
