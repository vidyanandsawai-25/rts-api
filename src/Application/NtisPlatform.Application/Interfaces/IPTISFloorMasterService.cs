using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces;

    public interface IPTISFloorMasterService
    {
    Task<IEnumerable<PTISFloorMasterEntity>> GetAllAsyncFloorMaster(CancellationToken cancellationToken = default);
    Task<PTISFloorMasterEntity> CreateAsyncFloorMaster(PTISFloorMasterDto dto, CancellationToken cancellationToken = default);
    Task<PTISFloorMasterEntity?> GetByIdAsyncFloorMaster(int id, CancellationToken cancellationToken = default);
    //Task<PTISFloorMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    //Task<Dictionary<string, object?>?> GetByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsyncFloorMaster(int id,PTISFloorMasterDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsyncFloorMaster(int id, CancellationToken cancellationToken = default);
}

