using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
namespace NtisPlatform.Application.Interfaces;

    public interface IPTISConstructionTypeMasterService
    {
    Task<IEnumerable<PTISConstructionTypeMasterEntity>> ConstructionTypeMasterGetAllAsync(CancellationToken cancellationToken = default);
    Task<PTISConstructionTypeMasterDtoResponse> ConstructionTypeMasterCreateAsync(PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken = default);
    //Task<List<PTISMasterEntity>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PTISConstructionTypeMasterEntity?> ConstructionTypeMasterGetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task ConstructionTypeMasterUpdateAsync(int id,PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken = default);
    Task ConstructionTypeMasterDeleteAsync(int id, CancellationToken cancellationToken = default);
	
}

