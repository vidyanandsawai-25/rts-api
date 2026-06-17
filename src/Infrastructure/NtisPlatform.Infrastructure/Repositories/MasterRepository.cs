using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Shared data-access implementation for master-record existence checks.
/// Each method is a single, side-effect-free existence query; the active-row predicate matches the
/// checks that were previously inlined inside <c>PropertyRepository</c>, so behaviour is unchanged.
/// </summary>
public class MasterRepository : IMasterRepository
{
    private readonly ApplicationDbContext _context;

    public MasterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> WardExistsAsync(int wardId, CancellationToken cancellationToken = default)
        => _context.WardMaster.AnyAsync(w => w.Id == wardId && w.IsActive, cancellationToken);

    public Task<bool> TaxZoneExistsAsync(int taxZoneId, CancellationToken cancellationToken = default)
        => _context.TaxZoneMaster.AnyAsync(tz => tz.Id == taxZoneId && tz.IsActive, cancellationToken);

    public Task<bool> MoujaExistsAsync(int moujaId, CancellationToken cancellationToken = default)
        => _context.MoujaEntity.AnyAsync(m => m.Id == moujaId && m.IsActive, cancellationToken);

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
        => _context.PropertyCategoryMaster.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);

    public Task<bool> PropertyTypeExistsAsync(int propertyTypeId, CancellationToken cancellationToken = default)
        => _context.PropertyTypeMasters.AnyAsync(pt => pt.Id == propertyTypeId && pt.IsActive, cancellationToken);

    public Task<bool> WingExistsAsync(int wingId, CancellationToken cancellationToken = default)
        => _context.Set<WingEntity>().AnyAsync(w => w.Id == wingId && w.IsActive, cancellationToken);

    public Task<bool> FloorExistsAsync(int floorId, CancellationToken cancellationToken = default)
        => _context.FloorEntity.AnyAsync(f => f.Id == floorId && f.IsActive, cancellationToken);

    public Task<bool> SubFloorExistsAsync(int subFloorId, CancellationToken cancellationToken = default)
        => _context.SubFloorEntity.AnyAsync(sf => sf.Id == subFloorId && sf.IsActive, cancellationToken);

    public Task<bool> ConstructionTypeExistsAsync(int constructionTypeId, CancellationToken cancellationToken = default)
        => _context.ConstructionTypeEntity.AnyAsync(c => c.Id == constructionTypeId && c.IsActive, cancellationToken);

    public Task<bool> TypeOfUseExistsAsync(int typeOfUseId, CancellationToken cancellationToken = default)
        => _context.TypeOfUse.AnyAsync(t => t.Id == typeOfUseId && t.IsActive, cancellationToken);

    public Task<bool> SubTypeOfUseExistsAsync(int subTypeOfUseId, CancellationToken cancellationToken = default)
        => _context.SubTypeOfUse.AnyAsync(stu => stu.Id == subTypeOfUseId && stu.IsActive, cancellationToken);
}
