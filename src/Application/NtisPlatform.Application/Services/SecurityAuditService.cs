using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Persists structured security audit events. Callers pass only non-sensitive metadata —
/// this service has no parameter through which a secret, OTP code, recovery code, password, or
/// token could ever be recorded.
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ISecurityAuditLogRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SecurityAuditService(ISecurityAuditLogRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task RecordAsync(
        string eventType,
        int? userId,
        bool success,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new SecurityAuditLogEntity
        {
            EventType = eventType,
            UserId = userId,
            Success = success,
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Detail = detail,
            CreatedAt = _timeProvider.GetLocalNow().DateTime
        };

        await _repository.AddAsync(entry, cancellationToken);
    }
}
