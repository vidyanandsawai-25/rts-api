using NtisPlatform.Application.DTOs.PropertySplit;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertySplitService : ICommonCrudService<PropertyMapDetailEntity, PropertySplitDto, CreatePropertySplitDto, UpdatePropertySplitDto, PropertySplitQueryParameters, int>
{

}
