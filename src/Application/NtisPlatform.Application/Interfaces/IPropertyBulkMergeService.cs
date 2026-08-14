using NtisPlatform.Application.DTOs.PropertyBulkMerge;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Interfaces;

public interface IPropertyBulkMergeService : ICommonCrudService<PropertyMapDetailEntity, PropertyBulkMergeDto, CreatePropertyBulkMergeDto, UpdatePropertyBulkMergeDto, PropertyBulkMergeQueryParameters, int>
{

}
