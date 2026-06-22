using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Infrastructure.Repositories;

public class TypeOfUseByPropertyTypeRepository : Repository<TypeOfUseEntity, int>, ITypeOfUseByPropertyTypeRepository
{
    public TypeOfUseByPropertyTypeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TypeOfUseEntity>> GetTypeOfUseByPropertyTypeIdAsync(int propertyTypeId, CancellationToken cancellationToken)
    {
        return await (from pdu in _context.PropertyDescriptionAndTypeOfUseValidations
                      join tum in _context.TypeOfUse on pdu.TypeOfUseId equals tum.Id
                      where pdu.PropertyTypeId == propertyTypeId
                            && pdu.IsActive
                            && tum.IsActive
                      select tum)
                     .ToListAsync(cancellationToken);
    }
}
