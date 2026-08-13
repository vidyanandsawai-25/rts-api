using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="IULBDocumentTypeService"/>
public class ULBDocumentTypeService : IULBDocumentTypeService
{
    private readonly IRepository<ULBDocumentTypeEntity, int> _typeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ULBDocumentTypeService(
        IRepository<ULBDocumentTypeEntity, int> typeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _typeRepository = typeRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<ULBDocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _typeRepository.GetQueryable().AsNoTracking()
            .OrderBy(t => t.DocumentTypeName)
            .Select(t => new ULBDocumentTypeDto
            {
                Id = t.Id,
                DocumentTypeCode = t.DocumentTypeCode,
                DocumentTypeName = t.DocumentTypeName,
                IsActive = t.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ULBDocumentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _typeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        return new ULBDocumentTypeDto
        {
            Id = entity.Id,
            DocumentTypeCode = entity.DocumentTypeCode,
            DocumentTypeName = entity.DocumentTypeName,
            IsActive = entity.IsActive
        };
    }

    public async Task<int> CreateAsync(CreateULBDocumentTypeDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var exists = await _typeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(t => t.DocumentTypeCode == dto.DocumentTypeCode, cancellationToken);
        if (exists)
            throw new ArgumentException($"Document type code '{dto.DocumentTypeCode}' already exists.", nameof(dto));

        var entity = new ULBDocumentTypeEntity
        {
            DocumentTypeCode = dto.DocumentTypeCode,
            DocumentTypeName = dto.DocumentTypeName,
            IsActive = true,
            CreatedBy = _currentUserService.GetCurrentUserId()
        };

        await _typeRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, UpdateULBDocumentTypeDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = await _typeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        entity.DocumentTypeName = dto.DocumentTypeName;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = _currentUserService.GetCurrentUserId();

        await _typeRepository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _typeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        entity.UpdatedBy = _currentUserService.GetCurrentUserId();

        await _typeRepository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
