using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using PropertyMergeDto = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyMergeDto;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyMergeService : ICommonCrudService<PropertyMapDetailEntity, PropertyMergeDto, CreatePropertyMergeDto, UpdatePropertyMergeDto, PropertyMergeQueryParameters, int>
{
   
}

