
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
namespace NtisPlatform.Application.Services;

public class PTISConstructionTypeMasterService : IPTISConstructionTypeMasterService
{
    private readonly IRepository<PTISConstructionTypeMasterEntity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PTISConstructionTypeMasterService(IRepository<PTISConstructionTypeMasterEntity> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<PTISConstructionTypeMasterEntity>> ConstructionTypeMasterGetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto);
    }

    public async Task<PTISConstructionTypeMasterDtoResponse> ConstructionTypeMasterCreateAsync(PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new PTISConstructionTypeMasterEntity
        {
            ConstructionId = dto.ConstructionId,
            Description = dto.Description,
            CreatedBy = 1,///We will use session UserID in future
            UpdatedBy = 1,///We will use session UserID in future
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        var created = await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PTISConstructionTypeMasterDtoResponse
        {
            Message = "Record saved Successfully"
        };
    }

    //public async Task<List<PTISMasterEntity>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    //{
    //    var entity = await _repository.ExecuteStoredProcedureAsync("EXEC PTIS.GetAllConstructionTypeData @Id",cancellationToken,new SqlParameter("@Id", id));
    //    return entity;
    //}
    public async Task<PTISConstructionTypeMasterEntity?> ConstructionTypeMasterGetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity;
    }


    public async Task ConstructionTypeMasterUpdateAsync(int id,PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Entity with ID {id} not found");
        }

        entity.ConstructionId = dto.ConstructionId;
        entity.Description = dto.Description;
        // entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    private static PTISConstructionTypeMasterEntity MapToDto(PTISConstructionTypeMasterEntity entity)
    {
        return new PTISConstructionTypeMasterEntity
        {
            ConstructionId = entity.ConstructionId,
            Description = entity.Description,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            CreatedDate = entity.CreatedDate,
            UpdatedDate = entity.UpdatedDate,
            Id= entity.Id
        };
    }

    public async Task ConstructionTypeMasterDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
	
	 
}

