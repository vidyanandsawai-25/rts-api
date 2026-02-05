using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;


public interface IMoujaService : ICommonCrudService<MoujaEntity, MoujaDto, CreateMoujaDto, UpdateMoujaDto, MoujaQueryParameters, int>
{
}
