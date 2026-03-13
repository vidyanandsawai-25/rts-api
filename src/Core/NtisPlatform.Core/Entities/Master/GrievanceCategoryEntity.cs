namespace NtisPlatform.Core.Entities.Master
{
    /// <summary>
    /// Entity representing a grievance category for complaint management
    /// </summary>
    public class GrievanceCategoryEntity : BaseEntity
    {
        /// <summary>
        /// Unique identifier for the grievance category
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique code identifying the grievance category
        /// </summary>
        public string CategoryCode { get; set; } = null!;

        /// <summary>
        /// Display name of the grievance category
        /// </summary>
        public string CategoryName { get; set; } = null!;

        /// <summary>
        /// Department responsible for handling this grievance category
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Priority level for this category (e.g., High, Medium, Low, Critical)
        /// </summary>
        public string Priority { get; set; } = null!;

        /// <summary>
        /// Service Level Agreement timeframe for resolution
        /// </summary>
        public string? ResolutionSla { get; set; }

        /// <summary>
        /// Escalation level if the grievance is not resolved within SLA
        /// </summary>
        public string? EscalationLevel { get; set; }

        /// <summary>
        /// Detailed description of the grievance category
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Navigation property to the department responsible for this category
        /// </summary>
        public DepartmentMasterEntity? Department { get; set; }
    }
}
