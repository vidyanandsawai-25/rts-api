using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class TypeOfUseByPropertyTypeMappingProfile : Profile
{
    public TypeOfUseByPropertyTypeMappingProfile()
    {
        CreateMap<TypeOfUseEntity, TypeOfUseByPropertyTypeItemDto>();
    }
}
