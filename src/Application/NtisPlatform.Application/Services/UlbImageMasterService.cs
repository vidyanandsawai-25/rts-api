using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services;

public class UlbImageMasterService
    : BaseCommonCrudService<UlbImageMasterEntity, UlbImageMasterDto, CreateUlbImageMasterDto, UpdateUlbImageMasterDto, UlbImageMasterQueryParameters, int>,
      IUlbImageMasterService
{
    private readonly IDocumentService _documentService;
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;

    public UlbImageMasterService(
        IRepository<UlbImageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDocumentService documentService,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository)
        : base(repository, unitOfWork, mapper)
    {
        _documentService = documentService;
        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _documentRepository = documentRepository;
        _documentBindingRepository = documentBindingRepository;
    }

    public override async Task<UlbImageMasterDto> CreateAsync(CreateUlbImageMasterDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto == null)
            throw new ArgumentNullException(nameof(createDto));

        // Validate document if provided
        if (createDto.ImageId.HasValue)
        {
            if (!createDto.CreatedBy.HasValue || createDto.CreatedBy.Value <= 0)
            {
                throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(createDto.CreatedBy), "User session is invalid or expired. Please log in again.", NtisPlatform.Application.Enums.OperationType.Create);
            }

            var document = await _documentService.GetDocumentByIdAsync(createDto.ImageId.Value, cancellationToken);
            if (document == null || !document.IsActive || document.MarkedForDeletion)
            {
                throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(createDto.ImageId), "The selected image is invalid, inactive, or has been deleted.", NtisPlatform.Application.Enums.OperationType.Create);
            }
        }

        // Begin transaction to ensure entity creation and document binding are atomic
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = _mapper.Map<UlbImageMasterEntity>(createDto);
            
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create binding if ImageId is provided
            if (createDto.ImageId.HasValue)
            {
                if (!createDto.DepartmentId.HasValue || createDto.DepartmentId <= 0)
                {
                    throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(createDto.DepartmentId), "Department selection is required when an image is selected.", NtisPlatform.Application.Enums.OperationType.Create);
                }
                if (!createDto.ModuleId.HasValue || createDto.ModuleId <= 0)
                {
                    throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(createDto.ModuleId), "Module selection is required when an image is selected.", NtisPlatform.Application.Enums.OperationType.Create);
                }
                var departmentId = createDto.DepartmentId.Value;
                var moduleId = createDto.ModuleId.Value;

                await _documentService.CreateDocumentBindingAsync(
                    createDto.ImageId.Value,
                    departmentId,
                    moduleId,
                    "UlbImageMaster",
                    entity.Id,
                    null,
                    "Id",
                    DocumentBindingPurpose.Photo.ToPurposeString(),
                    true,
                    departmentId,
                    createDto.CreatedBy,
                    createDto.CreatedBy.Value,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            var result = _mapper.Map<UlbImageMasterDto>(entity);
            await PopulateDocumentGuidAsync(result, cancellationToken);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<UlbImageMasterDto?> UpdateAsync(int id, UpdateUlbImageMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        if (updateDto == null)
            throw new ArgumentNullException(nameof(updateDto));

        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        var oldImageId = entity.ImageId;

        // Validate new document if changed
        if (updateDto.ImageId.HasValue && updateDto.ImageId != oldImageId)
        {
            if (!updateDto.UpdatedBy.HasValue || updateDto.UpdatedBy.Value <= 0)
            {
                throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(updateDto.UpdatedBy), "User session is invalid or expired. Please log in again.", NtisPlatform.Application.Enums.OperationType.Update);
            }

            var document = await _documentService.GetDocumentByIdAsync(updateDto.ImageId.Value, cancellationToken);
            if (document == null || !document.IsActive || document.MarkedForDeletion)
            {
                throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(updateDto.ImageId), "The selected image is invalid, inactive, or has been deleted.", NtisPlatform.Application.Enums.OperationType.Update);
            }
        }

        // Begin transaction to ensure entity update and document binding are atomic
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _mapper.Map(updateDto, entity);
            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create new binding if ImageId changed and is provided
            if (updateDto.ImageId.HasValue && updateDto.ImageId != oldImageId)
            {
                if (!updateDto.DepartmentId.HasValue || updateDto.DepartmentId <= 0)
                {
                    throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(updateDto.DepartmentId), "Department selection is required when an image is selected.", NtisPlatform.Application.Enums.OperationType.Update);
                }
                if (!updateDto.ModuleId.HasValue || updateDto.ModuleId <= 0)
                {
                    throw new NtisPlatform.Application.Exceptions.ValidationException(nameof(updateDto.ModuleId), "Module selection is required when an image is selected.", NtisPlatform.Application.Enums.OperationType.Update);
                }
                var departmentId = updateDto.DepartmentId.Value;
                var moduleId = updateDto.ModuleId.Value;

                await UnmarkExistingPrimaryBindingsAsync(entity.Id, cancellationToken);

                await _documentService.CreateDocumentBindingAsync(
                    updateDto.ImageId.Value,
                    departmentId,
                    moduleId,
                    "UlbImageMaster",
                    entity.Id,
                    null,
                    "Id",
                    DocumentBindingPurpose.Photo.ToPurposeString(),
                    true,
                    departmentId,
                    updateDto.UpdatedBy,
                    updateDto.UpdatedBy.Value,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            var result = _mapper.Map<UlbImageMasterDto>(entity);
            await PopulateDocumentGuidAsync(result, cancellationToken);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<UlbImageMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await base.GetByIdAsync(id, cancellationToken);
        await PopulateDocumentGuidAsync(dto, cancellationToken);
        return dto;
    }

    public override async Task<PagedResult<UlbImageMasterDto>> GetAllAsync(UlbImageMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        if (result?.Items != null)
        {
            await PopulateDocumentGuidsAsync(result.Items, cancellationToken);
        }
        return result!;
    }

    private async Task PopulateDocumentGuidAsync(UlbImageMasterDto? dto, CancellationToken cancellationToken)
    {
        if (dto != null && dto.ImageId.HasValue)
        {
            var doc = await _documentService.GetDocumentByIdAsync(dto.ImageId.Value, cancellationToken);
            if (doc != null && doc.IsActive && !doc.MarkedForDeletion)
            {
                dto.DocumentGuid = doc.DocumentGuid;
            }
        }
    }

    private async Task PopulateDocumentGuidsAsync(IEnumerable<UlbImageMasterDto> dtos, CancellationToken cancellationToken)
    {
        var imageIds = dtos
            .Where(d => d.ImageId.HasValue)
            .Select(d => d.ImageId!.Value)
            .Distinct()
            .ToList();

        if (imageIds.Count == 0)
            return;

        var documents = await _documentRepository.GetAsync(
            d => imageIds.Contains(d.Id) && d.IsActive && !d.MarkedForDeletion,
            cancellationToken);

        var docGuidMap = documents.ToDictionary(d => d.Id, d => d.DocumentGuid);

        foreach (var dto in dtos)
        {
            if (dto.ImageId.HasValue && docGuidMap.TryGetValue(dto.ImageId.Value, out var guid))
            {
                dto.DocumentGuid = guid;
            }
        }
    }




    private async Task UnmarkExistingPrimaryBindingsAsync(int referenceId, CancellationToken cancellationToken)
    {
        var existingPrimaryBindings = await _documentBindingRepository.GetAsync(
            b => b.ReferenceTableName == "UlbImageMaster" &&
                 b.ReferenceTableId == referenceId &&
                 b.IsPrimaryDocument &&
                 b.IsActive &&
                 !b.MarkedForDeletion,
            cancellationToken);

        foreach (var binding in existingPrimaryBindings)
        {
            binding.UnmarkAsPrimary();
            await _documentBindingRepository.UpdateAsync(binding, cancellationToken);
        }
    }

    public async Task<bool> IsUlbImageDocumentAsync(Guid documentGuid, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable()
            .Join(_documentRepository.GetQueryable(),
                img => img.ImageId,
                doc => doc.Id,
                (img, doc) => new { img, doc });

        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            query,
            x => x.doc.DocumentGuid == documentGuid && x.img.IsActive && x.doc.IsActive && !x.doc.MarkedForDeletion,
            cancellationToken);
    }
}
