using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Services;

public class PTISFloorMasterService: IPTISFloorMasterService
{
    private readonly IRepository<PTISFloorMasterEntity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PTISFloorMasterService(IRepository<PTISFloorMasterEntity> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PTISFloorMasterEntity>> GetAllAsyncFloorMaster(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDtoFloorMaster);
    }

    public async Task<PTISFloorMasterEntity> CreateAsyncFloorMaster(PTISFloorMasterDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new PTISFloorMasterEntity
        {
            FloorID = dto.FloorID,
            Description = dto.Description,
            CreatedBy = dto.CreatedBy,
            CreatedDate = DateTime.Now,
            UpdatedBy = dto.UpdatedBy,
            UpdatedDate = DateTime.Now
        };

        var created = await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDtoFloorMaster(created);
    }

    public async Task<PTISFloorMasterEntity?> GetByIdAsyncFloorMaster(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToDtoFloorMaster(entity);
    }


    public async Task UpdateAsyncFloorMaster(int id,PTISFloorMasterDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Entity with ID {id} not found");
        }

        entity.FloorID = dto.FloorID;
        entity.Description = dto.Description;
        entity.CreatedBy = dto.CreatedBy;
        entity.CreatedDate = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsyncFloorMaster(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PTISFloorMasterEntity MapToDtoFloorMaster(PTISFloorMasterEntity entity)
    {
        return new PTISFloorMasterEntity
        {
            FloorID = entity.FloorID,
            Description = entity.Description,
            CreatedBy = entity.CreatedBy,
            CreatedDate = entity.CreatedDate,
            UpdatedBy = entity.UpdatedBy,
            UpdatedDate = entity.UpdatedDate,
            Id = entity.Id
        };
    }


}

