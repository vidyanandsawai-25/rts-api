namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>
/// Carries the acting user's identity and HTTP audit context (source IP, device) from the
/// controller into the service. Keeps actor identity out of the request body.
/// </summary>
public record OperationContext(
    int ActingUserId,
    string? UserName = null,
    string? UserRole = null,
    string? SourceIp = null,
    string? UserAgent = null);
