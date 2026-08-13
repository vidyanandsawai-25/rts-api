using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Hand-rolled service for ULB document metadata rows. Follows the mandatory
/// create-row-then-upload-then-auto-link Document/DocumentBinding pattern (see
/// <see cref="IULBDocumentService"/> remarks and <c>ULBDocumentBindingHandler</c>).
///
/// <para>
/// Deliberately has NO dependency on <c>IDocumentApplicationService</c>: that service resolves
/// every registered <c>IDocumentBindingHandler</c>, including <c>ULBDocumentBindingHandler</c>,
/// which itself depends on this interface — adding the dependency here would create a DI cycle.
/// File-metadata joining for display is done by the controller instead, which can safely depend on
/// both services.
/// </para>
/// </summary>
public class ULBDocumentService : IULBDocumentService
{
    private readonly IRepository<ULBDocumentEntity, int> _documentRepository;
    private readonly IRepository<ULBDocumentTypeEntity, int> _typeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ULBDocumentService> _logger;

    public ULBDocumentService(
        IRepository<ULBDocumentEntity, int> documentRepository,
        IRepository<ULBDocumentTypeEntity, int> typeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<ULBDocumentService> logger)
    {
        _documentRepository = documentRepository;
        _typeRepository = typeRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<List<ULBDocumentDto>> GetLatestAsync(string? typeCodes, CancellationToken cancellationToken = default)
    {
        var documentTypeCodes = string.IsNullOrWhiteSpace(typeCodes)
            ? null
            : typeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var query = from d in _documentRepository.GetQueryable().AsNoTracking()
                    join t in _typeRepository.GetQueryable().AsNoTracking() on d.ULBDocumentTypeId equals t.Id
                    where !d.MarkedForDeletion && d.IsActive && d.IsLatest
                    select new { Doc = d, TypeCode = t.DocumentTypeCode, TypeName = t.DocumentTypeName };

        if (documentTypeCodes is { Length: > 0 })
            query = query.Where(x => documentTypeCodes.Contains(x.TypeCode));

        var rows = await query.ToListAsync(cancellationToken);

        return rows.Select(x => new ULBDocumentDto
        {
            Id = x.Doc.Id,
            DocumentTypeCode = x.TypeCode,
            DocumentTypeName = x.TypeName,
            DocumentBindingId = x.Doc.DocumentBindingId
        }).ToList();
    }

    public async Task<int> CreateAsync(CreateULBDocumentDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = _currentUserService.GetCurrentUserId();

        var type = await _typeRepository.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(t => t.DocumentTypeCode == dto.DocumentTypeCode && t.IsActive, cancellationToken);

        if (type == null)
            throw new ArgumentException($"Document type '{dto.DocumentTypeCode}' does not exist or is inactive.", nameof(dto));

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var priorLatest = await _documentRepository.GetQueryable()
                .Where(d => d.ULBDocumentTypeId == type.Id && d.IsLatest && !d.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            foreach (var prior in priorLatest)
            {
                prior.IsLatest = false;
                await _documentRepository.UpdateAsync(prior, cancellationToken);
            }

            var entity = new ULBDocumentEntity
            {
                ULBDocumentTypeId = type.Id,
                DocumentBindingId = null,
                IsLatest = true,
                IsActive = true,
                CreatedBy = userId
            };

            await _documentRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return entity.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task LinkDocumentBindingAsync(int id, int documentBindingId, int userId, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            _logger.LogWarning("LinkDocumentBindingAsync: no ULBDocument found with Id={Id}.", id);
            return;
        }

        entity.DocumentBindingId = documentBindingId;
        entity.UpdatedBy = userId;
        await _documentRepository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkDocumentBindingAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return;

        entity.DocumentBindingId = null;
        entity.UpdatedBy = userId;
        await _documentRepository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null || entity.MarkedForDeletion)
            return false;

        entity.UpdatedBy = _currentUserService.GetCurrentUserId();
        await _documentRepository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        return entity != null && !entity.MarkedForDeletion;
    }

    public Task<ULBDocumentEntity?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default)
        => _documentRepository.GetByIdAsync(id, cancellationToken);
}
