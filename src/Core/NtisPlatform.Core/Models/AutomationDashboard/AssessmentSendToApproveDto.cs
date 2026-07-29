namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Request to send one or more Assessment properties to Clerk approval.
/// </summary>
public class SendToApproveRequestDto
{
    /// <summary>
    /// Property ids to send for approval. Send one id for a single property or multiple ids for bulk approval.
    /// </summary>
    public List<int> PropertyIds { get; set; } = new();

    public int UserId { get; set; }
}

/// <summary>
/// Response after sending Assessment properties to approval.
/// </summary>
public class SendToApproveResponseDto
{
    public bool IsInserted { get; set; }
    public int PropertyId { get; set; }
    public List<int> PropertyIds { get; set; } = new();
    public int UserId { get; set; }
    public int SignAuthorityId { get; set; }
    public string AuthorityCode { get; set; } = string.Empty;
    public int RequestedCount { get; set; }
    public int InsertedCount { get; set; }
    public List<int> InsertedPropertyIds { get; set; } = new();
    public List<int> MissingPropertyIds { get; set; } = new();
    public List<int> AlreadySentPropertyIds { get; set; } = new();
    public List<int> InvalidPropertyIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
