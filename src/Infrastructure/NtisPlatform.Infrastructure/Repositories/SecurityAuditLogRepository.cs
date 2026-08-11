using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Security audit log repository implementation. Append-only — no update/delete surface is
/// exposed since audit records must remain immutable.
/// </summary>
public class SecurityAuditLogRepository : ISecurityAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public SecurityAuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SecurityAuditLogEntity entry, CancellationToken cancellationToken = default)
    {
        await _context.Set<SecurityAuditLogEntity>().AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
