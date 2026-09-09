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
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.WingDetailId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null; // property not found

        var result = await (
            from s in _context.SocietyDetailsMast.AsNoTracking()
            where s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion
            // Tied to this property's own WingDetailId/PropertyId, not just "any wing under
            // the society" -- a society can have multiple wings, so joining by SocietyDetailsMastId/
            // SocietyDetailId alone (with no per-property ordering) could return an arbitrary one.
            join wdmGroup in _context.Set<NtisPlatform.Core.Entities.WingDetailsMastEntity>().AsNoTracking() on property.WingDetailId equals (int?)wdmGroup.Id into wdmJoin
            from wdm in wdmJoin.Where(x => x.IsActive && !x.MarkedForDeletion).DefaultIfEmpty()
            join swdGroup in _context.Set<NtisPlatform.Core.Entities.SocietyWingDetailsEntity>().AsNoTracking() on (int?)propertyId equals swdGroup.PropertyId into swdJoin
            from swd in swdJoin.Where(x => x.IsActive).DefaultIfEmpty()
            join wingGroup in _context.Set<WingEntity>().AsNoTracking() on
                (wdm != null ? (int?)wdm.WingMasterId : (swd != null ? swd.WingId : null)) equals (int?)wingGroup.Id into wingJoin
            from w in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
            select new PropertySocietyDetailsDto
            {
                PropertyId = propertyId,
                SocietyDetailId = s.Id,
                WingId = wdm != null ? (int?)wdm.WingMasterId : (swd != null ? swd.WingId : (w != null ? (int?)w.Id : null)),
                // WingNo is WingEntity.WingNo specifically -- no name-field fallback, since
                // WingDetailsMast.WingName / SocietyWingDetails.NewWingName are names, not codes.
                WingNo = w != null ? w.WingNo : null,
                WingName = wdm != null ? wdm.WingName : (swd != null ? swd.NewWingName : null),
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

    /// <inheritdoc/>
    public async Task<WingDetailsMastEntity?> GetWingDetailsMastBySocietyIdAsync(int societyId, CancellationToken cancellationToken = default)
        => await _context.Set<WingDetailsMastEntity>()
            .FirstOrDefaultAsync(w => w.SocietyDetailsMastId == societyId && w.IsActive && !w.MarkedForDeletion, cancellationToken);

    /// <inheritdoc/>
    public void AddWingDetailsMast(WingDetailsMastEntity wingDetailsMast)
        => _context.Set<WingDetailsMastEntity>().Add(wingDetailsMast);
}
