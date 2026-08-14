namespace NtisPlatform.Application.DTOs.LockUnlock;

public class BulkLockResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }

    /// <summary>
    /// Pairs whose PropertyScreenLock row already matched the requested action (already
    /// locked/unlocked, active, not soft-deleted) - skipped without writing, not counted in
    /// SuccessCount. TotalRequested = SuccessCount + AlreadyInStateCount + FailedCount.
    /// </summary>
    public int AlreadyInStateCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
