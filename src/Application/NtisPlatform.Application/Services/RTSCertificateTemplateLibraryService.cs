using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSCertificateTemplateLibraryService : IRTSCertificateTemplateLibraryService
{
    private const int MaxDesignJsonLength = 5 * 1024 * 1024;
    private readonly IRepository<RTSCertificateCoreTemplateMasterEntity, int> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RTSCertificateTemplateLibraryService(
        IRepository<RTSCertificateCoreTemplateMasterEntity, int> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RTSCertificateLibraryTemplateDto>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(template => !template.MarkedForDeletion)
            .OrderByDescending(template => template.UpdatedDate ?? template.CreatedDate)
            .ToListAsync(ct);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<RTSCertificateLibraryTemplateDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(template => template.Id == id && !template.MarkedForDeletion, ct);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<RTSCertificateLibraryTemplateDto> CreateAsync(
        CreateRTSCertificateLibraryTemplateDto dto,
        int userId,
        CancellationToken ct)
    {
        Validate(dto.TemplateName, dto.TemplateCode, dto.Description, dto.BodyContent, dto.DesignJson);
        var normalizedCode = dto.TemplateCode.Trim().ToUpperInvariant();
        await EnsureCodeAvailableAsync(normalizedCode, null, dto.IsActive, ct);

        var entity = new RTSCertificateCoreTemplateMasterEntity
        {
            TemplateName = dto.TemplateName.Trim(),
            TemplateCode = normalizedCode,
            Description = NormalizeOptional(dto.Description),
            HeaderContent = dto.HeaderContent,
            BodyContent = dto.BodyContent,
            FooterContent = dto.FooterContent,
            DesignJson = dto.DesignJson,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<RTSCertificateLibraryTemplateDto> UpdateAsync(
        UpdateRTSCertificateLibraryTemplateDto dto,
        int userId,
        CancellationToken ct)
    {
        var entity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(template => template.Id == dto.Id && !template.MarkedForDeletion, ct)
            ?? throw new KeyNotFoundException($"Certificate template with ID {dto.Id} not found.");

        Validate(dto.TemplateName, dto.TemplateCode, dto.Description, dto.BodyContent, dto.DesignJsonSpecified ? dto.DesignJson : entity.DesignJson);
        var normalizedCode = dto.TemplateCode.Trim().ToUpperInvariant();
        await EnsureCodeAvailableAsync(normalizedCode, entity.Id, dto.IsActive, ct);

        entity.TemplateName = dto.TemplateName.Trim();
        entity.TemplateCode = normalizedCode;
        entity.Description = NormalizeOptional(dto.Description);
        entity.HeaderContent = dto.HeaderContent;
        entity.BodyContent = dto.BodyContent;
        entity.FooterContent = dto.FooterContent;
        if (dto.DesignJsonSpecified)
        {
            entity.DesignJson = dto.DesignJson;
        }
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var entity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(template => template.Id == id && !template.MarkedForDeletion, ct);

        if (entity == null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.UtcNow;
        await _repository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureCodeAvailableAsync(string code, int? currentId, bool isActive, CancellationToken ct)
    {
        if (!isActive)
        {
            return;
        }

        var exists = await _repository.GetQueryable().AsNoTracking().AnyAsync(
            template => template.TemplateCode == code
                && template.IsActive
                && !template.MarkedForDeletion
                && (!currentId.HasValue || template.Id != currentId.Value),
            ct);

        if (exists)
        {
            throw new InvalidOperationException($"An active certificate template with code '{code}' already exists.");
        }
    }

    private static void Validate(string name, string code, string? description, string bodyContent, string? designJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Template name cannot exceed 200 characters.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Template code is required.", nameof(code));
        if (code.Trim().Length > 50)
            throw new ArgumentException("Template code cannot exceed 50 characters.", nameof(code));
        if (description?.Trim().Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        if (string.IsNullOrWhiteSpace(bodyContent))
            throw new ArgumentException("Body content is required.", nameof(bodyContent));
        if (designJson is null)
            return;
        if (designJson.Length > MaxDesignJsonLength)
            throw new ArgumentException($"DesignJson cannot exceed {MaxDesignJsonLength} characters.", nameof(designJson));

        try
        {
            using var document = JsonDocument.Parse(designJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("DesignJson root value must be a JSON object.", nameof(designJson));
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("DesignJson must contain valid JSON.", nameof(designJson), exception);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RTSCertificateLibraryTemplateDto MapToDto(RTSCertificateCoreTemplateMasterEntity entity) => new()
    {
        Id = entity.Id,
        TemplateName = entity.TemplateName,
        TemplateCode = entity.TemplateCode,
        Description = entity.Description,
        HeaderContent = entity.HeaderContent,
        BodyContent = entity.BodyContent,
        FooterContent = entity.FooterContent,
        DesignJson = entity.DesignJson,
        IsActive = entity.IsActive,
        CreatedDate = entity.CreatedDate ?? DateTime.UtcNow,
        UpdatedDate = entity.UpdatedDate
    };
}
