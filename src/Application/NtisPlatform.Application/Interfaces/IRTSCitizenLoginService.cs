using NtisPlatform.Application.DTOs.CitizenLoginDetails;
using NtisPlatform.Core.Entities.PropertyTax;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSCitizenLoginService : ICommonCrudService<RTSPropertyMastEntity , PropertyDto , CreatePropertyDto , UpdatePropertyDto, PropertyQueryParameters , int>
{
}
