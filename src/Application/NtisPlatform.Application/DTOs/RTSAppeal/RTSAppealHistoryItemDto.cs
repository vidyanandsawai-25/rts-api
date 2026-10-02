using System;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Historical timeline audit entry for an RTS appeal or underlying application.
/// </summary>
public class RTSAppealHistoryItemDto
{
    public int Id { get; set; }
    public string? StageName { get; set; }
    public string? Action { get; set; }
    public string? ActionByName { get; set; }
    public string? Status { get; set; }
    public string? Remark { get; set; }
    public DateTime? ActionDate { get; set; }
}
