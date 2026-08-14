using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="IULBDocumentQueryService"/>
public class ULBDocumentQueryService : IULBDocumentQueryService
{
    private readonly IULBDocumentService _ulbDocumentService;
    private readonly IRepository<DocumentBindingEntity, int> _bindingRepository;

    public ULBDocumentQueryService(
        IULBDocumentService ulbDocumentService,
        IRepository<DocumentBindingEntity, int> bindingRepository)
    {
        _ulbDocumentService = ulbDocumentService;
        _bindingRepository = bindingRepository;
    }

    public async Task<List<ULBDocumentDto>> GetLatestAsync(string? typeCodes, CancellationToken cancellationToken = default)
    {
        var result = await _ulbDocumentService.GetLatestAsync(typeCodes, cancellationToken);

        var bindingIds = result
            .Where(d => d.DocumentBindingId.HasValue)
            .Select(d => d.DocumentBindingId!.Value)
            .Distinct()
            .ToList();

        if (bindingIds.Count > 0)
        {
            var bindings = await _bindingRepository.GetQueryable().AsNoTracking()
                .Include(b => b.Document)
                .Where(b => bindingIds.Contains(b.Id)
                    && b.IsActive
                    && !b.MarkedForDeletion
                    && b.Document != null
                    && b.Document.IsActive
                    && !b.Document.MarkedForDeletion)
                .ToDictionaryAsync(b => b.Id, b => b.Document!, cancellationToken);

            foreach (var dto in result)
            {
                if (dto.DocumentBindingId.HasValue && bindings.TryGetValue(dto.DocumentBindingId.Value, out var document))
                {
                    dto.OriginalFileName = document.OriginalFileName;
                    dto.MimeType = document.MimeType;
                    dto.FileSizeBytes = document.FileSizeBytes;
                    dto.DocumentGuid = document.DocumentGuid;
                }
            }
        }

        return result;
    }
}
