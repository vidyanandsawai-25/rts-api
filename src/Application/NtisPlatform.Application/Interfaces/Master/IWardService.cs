using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IWardService : ICommonCrudService<WardEntity, WardDto, CreateWardDto, UpdateWardDto, WardQueryParameters, string>
{
}

