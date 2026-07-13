using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface IUlbImageMasterService
    : ICommonCrudService<UlbImageMasterEntity, UlbImageMasterDto, CreateUlbImageMasterDto, UpdateUlbImageMasterDto, UlbImageMasterQueryParameters, int>
{


    Task<bool> IsUlbImageDocumentAsync(Guid documentGuid, CancellationToken cancellationToken = default);
}
