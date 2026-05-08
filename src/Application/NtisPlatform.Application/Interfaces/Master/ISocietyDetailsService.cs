using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ISocietyDetailsService : ICommonCrudService<SocietyDetailsEntity, SocietyDetailsDto, CreateSocietyDetailsDto, UpdateSocietyDetailsDto, SocietyDetailsQueryParameters, int>
{
}
