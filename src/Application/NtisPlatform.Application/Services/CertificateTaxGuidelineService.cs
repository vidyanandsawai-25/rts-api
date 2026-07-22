using System.Globalization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class CertificateTaxGuidelineService
    : BaseCommonCrudService<CertificateTaxGuidelineEntity, CertificateTaxGuidelineDto, CreateCertificateTaxGuidelineDto, UpdateCertificateTaxGuidelineDto, CertificateTaxGuidelineQueryParameters, int>,
      ICertificateTaxGuidelineService
{
    private readonly IReferenceValidationService _referenceValidator;

    public CertificateTaxGuidelineService(
        IRepository<CertificateTaxGuidelineEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    public async Task<object?> GetGuidelineValueAsync(string code, CancellationToken cancellationToken = default)
    {
        var guideline = await _repository.GetQueryable()
            .FirstOrDefaultAsync(g => g.GuidelineCode == code && g.IsActive, cancellationToken);

        if (guideline == null)
            return null;

        return ConvertValue(guideline.GuidelineValue, guideline.DataType);
    }

    public async Task<Dictionary<string, object?>> GetGuidelineValuesByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        var guidelines = await _repository.GetQueryable()
            .Where(g => g.GuidelineGroup == group && g.IsActive)
            .OrderBy(g => g.DisplayOrder ?? 0)
            .ToListAsync(cancellationToken);

        return guidelines.ToDictionary(
            g => g.GuidelineCode,
            g => ConvertValue(g.GuidelineValue, g.DataType)
        );
    }

    private static object? ConvertValue(string? value, string dataType)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return dataType.ToUpperInvariant() switch
        {
            "BIT" => value == "1" ? true :
                     value == "0" ? false :
                     (bool.TryParse(value, out var b) ? b : (bool?)null),
            "INT" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null,
            "DECIMAL" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null,
            _ => value
        };
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        CertificateTaxGuidelineEntity currentEntity,
        CertificateTaxGuidelineEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<CertificateTaxGuidelineEntity>(id, cancellationToken);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        CertificateTaxGuidelineEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<CertificateTaxGuidelineEntity>(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CertificateTaxGuidelineDto>> BulkUpsertAsync(
        IReadOnlyList<UpdateCertificateTaxGuidelineDto> items,
        CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
            return Array.Empty<CertificateTaxGuidelineDto>();

        var codes = items
            .Select(i => i.GuidelineCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        // Load only the rows we might update (keyed by GuidelineCode)
        var existingByCode = await _repository.GetQueryable()
            .Where(e => codes.Contains(e.GuidelineCode))
            .ToDictionaryAsync(e => e.GuidelineCode, cancellationToken);

        var results = new List<CertificateTaxGuidelineDto>(items.Count);

        foreach (var dto in items)
        {
            if (existingByCode.TryGetValue(dto.GuidelineCode, out var existing))
            {
                // Update existing row — map only the mutable fields
                existing.GuidelineName        = dto.GuidelineName;
                existing.Description          = dto.Description;
                existing.GuidelineGroup       = dto.GuidelineGroup ?? string.Empty;
                existing.DisplayOrder         = dto.DisplayOrder;
                existing.DataType             = dto.DataType;
                existing.GuidelineValue       = dto.GuidelineValue;
                existing.AllowedValues        = dto.AllowedValues;
                existing.IsActive             = dto.IsActive;
                existing.UpdatedBy            = dto.UpdatedBy;

                await _repository.UpdateAsync(existing, cancellationToken);
                results.Add(_mapper.Map<CertificateTaxGuidelineDto>(existing));
            }
            else
            {
                // Create new row
                var newEntity = _mapper.Map<CertificateTaxGuidelineEntity>(dto);
                await _repository.AddAsync(newEntity, cancellationToken);
                results.Add(_mapper.Map<CertificateTaxGuidelineDto>(newEntity));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return results.AsReadOnly();
    }
}
