using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// Query parameters for filtering rule version history
    /// </summary>
    public class RuleVersionHistoryQueryParameters : BaseQueryParameters
    {
        public int? RuleId { get; set; }
        public string? RuleCode { get; set; }
        public string? ChangeType { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedFrom { get; set; }
        public DateTime? ChangedTo { get; set; }
    }
}
