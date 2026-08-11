using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for persisting structured security audit records.
/// </summary>
public interface ISecurityAuditLogRepository
{
    Task AddAsync(SecurityAuditLogEntity entry, CancellationToken cancellationToken = default);
}
