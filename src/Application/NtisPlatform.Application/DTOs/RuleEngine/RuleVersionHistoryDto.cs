namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// DTO for retrieving rule version history
    /// </summary>
    public class RuleVersionHistoryDto
    {
        public long Id { get; set; }
        public int RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public int Version { get; set; }

        // Snapshot of Rule at this version
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RuleJson { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }

        // Change Metadata
        public string ChangeType { get; set; } = string.Empty;
        public string? ChangeReason { get; set; }
        public int ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? ChangeSummary { get; set; }
    }
}
