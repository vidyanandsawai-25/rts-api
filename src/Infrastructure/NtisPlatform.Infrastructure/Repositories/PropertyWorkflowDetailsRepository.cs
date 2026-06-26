using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

public class PropertyWorkflowDetailsRepository : Repository<PropertyWorkflowDetailsEntity, int>, IPropertyWorkflowDetailsRepository
{
    public PropertyWorkflowDetailsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task ResetCurrentStatusAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PropertyWorkflowDetails
            .Where(x => x.PropertyId == propertyId && x.CurrentStatus == true)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
        {
            item.CurrentStatus = false;
            item.UpdatedDate = DateTime.Now;
            _context.Entry(item).State = EntityState.Modified;
        }
    }

    public async Task<List<PropertyWorkflowDetailsEntity>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyWorkflowDetails
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyWorkflowDetailsEntity?> GetCurrentByPropertyNoAsync(string propertyid, CancellationToken cancellationToken = default)
    {
        return await (
            from wd in _context.PropertyWorkflowDetails
            join pm in _context.PropertyMast on wd.PropertyId equals pm.Id          
            where wd.PropertyId.ToString() == propertyid && wd.CurrentStatus == true
            select wd
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
