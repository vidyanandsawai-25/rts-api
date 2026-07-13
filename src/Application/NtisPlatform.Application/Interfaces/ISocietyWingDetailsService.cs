using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ISocietyWingDetailsService : ICommonCrudService<SocietyWingDetailsEntity, SocietyWingDetailsDto, CreateSocietyWingDetailsDto, UpdateSocietyWingDetailsDto, SocietyWingDetailsQueryParameters, int>
{
}
