using NtisPlatform.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Core.Interfaces;

public interface ITypeOfUseByPropertyTypeRepository : IRepository<TypeOfUseEntity, int>
{
    Task<IEnumerable<TypeOfUseEntity>> GetTypeOfUseByPropertyTypeIdAsync(int propertyTypeId, CancellationToken cancellationToken);
}
