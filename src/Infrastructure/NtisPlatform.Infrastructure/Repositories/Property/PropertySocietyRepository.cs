using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Data-access implementation for the Property "Society Details" tab.
/// All read-only queries use AsNoTracking and project only the columns required for the DTO.
/// Tracked entity loads are reserved for the write path (mutation in the service).
/// The "empty DTO when society is missing" decision belongs to the service, not here.
/// </summary>
public class PropertySocietyRepository : PropertyRepositoryBase, IPropertySocietyRepository
{
    public PropertySocietyRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Project only the two columns needed to decide which society row to load.
        var prop = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.SocietyDetailId })
            .FirstOrDefaultAsync(cancellationToken);

        if (prop == null)
            return null; // property not found

        if (!prop.SocietyDetailId.HasValue)
            return null; // society not linked yet — service will return an empty DTO

        var result = await (
            from s in _context.SocietyDetailsMast.AsNoTracking()
            where s.Id == prop.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion
            join w in _context.Set<WingEntity>().AsNoTracking() on s.WingId equals w.Id into wingJoin
            from w in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
            select new PropertySocietyDetailsDto
            {
                PropertyId = prop.Id,
                SocietyDetailId = s.Id,
                WingId = s.WingId,
                WingNo = w != null ? w.WingNo : null,
                WingName = s.WingName,
                SocietyName = s.SocietyName,
                SocietyAddress = s.SocietyAddress,
                SecretaryName = s.SecretaryName,
                ManagerName = s.ManagerName,
                LandOwnerName = s.LandOwnerName,
                BuilderName = s.BuilderName,
                SocietyNameEnglish = s.SocietyNameEnglish,
                SocietyAddressEnglish = s.SocietyAddressEnglish,
                SecretaryNameEnglish = s.SecretaryNameEnglish,
                ManagerNameEnglish = s.ManagerNameEnglish,
                LandOwnerNameEnglish = s.LandOwnerNameEnglish,
                BuilderNameEnglish = s.BuilderNameEnglish,
                ManagerMobileNo = s.ManagerMobileNo,
                SecretaryMobileNo = s.SecretaryMobileNo,
                SocietyEmailId = s.SocietyEmailId,
                SecretaryEmailId = s.SecretaryEmailId,
                ManagerEmailId = s.ManagerEmailId
            })
            .FirstOrDefaultAsync(cancellationToken);

        // null here means the FK-referenced society row was deleted/deactivated.
        // The service decides what to do (return empty DTO in that case too).
        return result;
    }

    /// <inheritdoc/>
    public Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _context.PropertyMast
            .AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

    /// <inheritdoc/>
    public Task<SocietyDetailsEntity?> GetSocietyByIdAsync(int societyId, CancellationToken cancellationToken = default)
        => _context.SocietyDetailsMast
            .FirstOrDefaultAsync(s => s.Id == societyId && s.IsActive && !s.MarkedForDeletion, cancellationToken);

    /// <inheritdoc/>
    public Task<SocietyDetailsEntity?> GetSocietyByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
        => _context.SocietyDetailsMast
            .FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion, cancellationToken);

    /// <inheritdoc/>
    public void AddSociety(SocietyDetailsEntity society)
        => _context.SocietyDetailsMast.Add(society);
}
