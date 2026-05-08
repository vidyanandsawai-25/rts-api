namespace NtisPlatform.Application.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Items { get; set; }
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Correlation ID for tracking errors and diagnostic purposes
    /// </summary>
    public string? CorrelationId { get; set; }
}
