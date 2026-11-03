using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSFieldDefinitionService:BaseCommonCrudService<RTSFieldDefinitionEntity, RTSFieldDefinitionDto, CreateRTSFieldDefinitionDto, UpdateRTSFieldDefinitionDto, RTSFieldDefinitionQueryParameters, int>, IRTSFieldDefinitionService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<RTSServiceEntity, int> _services;
    private readonly IRepository<RTSDepartmentEntity, int> _departments;

    public RTSFieldDefinitionService(
        IRepository<RTSFieldDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<RTSServiceEntity, int> services,
        IRepository<RTSDepartmentEntity, int> departments): base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _services = services;
        _departments = departments;
    }

    public async Task<PagedResult<RTSFieldDefinitionServiceCardDto>> GetServiceCardsAsync(
        RTSFieldDefinitionServiceCardQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var cards =
            from service in _services.GetQueryable().AsNoTracking()
            join department in _departments.GetQueryable().AsNoTracking()
                on service.DepartmentId equals department.Id
            join field in _repository.GetQueryable().AsNoTracking()
                on new { ServiceId = service.Id, DepartmentId = service.DepartmentId }
                equals new { field.ServiceId, field.DepartmentId }
            where !field.MarkedForDeletion
                && service.IsActive
                && department.IsActive
                && (!query.DepartmentId.HasValue || service.DepartmentId == query.DepartmentId.Value)
                && (!query.ServiceId.HasValue || service.Id == query.ServiceId.Value)
            group field by new
            {
                service.Id,
                service.ServiceName,
                service.DepartmentId,
                department.DepartmentName
            }
            into grouped
            select new RTSFieldDefinitionServiceCardDto
            {
                ServiceId = grouped.Key.Id,
                ServiceName = grouped.Key.ServiceName,
                DepartmentId = grouped.Key.DepartmentId,
                DepartmentName = grouped.Key.DepartmentName,
                FieldCount = grouped.Count(),
                ActiveFieldCount = grouped.Count(field => field.IsActive),
                SectionCount = grouped
                    .Select(field => string.IsNullOrWhiteSpace(field.FieldGroup)
                        ? "__general__"
                        : field.FieldGroup!.Trim())
                    .Distinct()
                    .Count()
            };

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            cards = cards.Where(card =>
                card.ServiceName.Contains(search) || card.DepartmentName.Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            cards = cards.Where(card => query.IsActive.Value
                ? card.ActiveFieldCount > 0
                : card.ActiveFieldCount < card.FieldCount);
        }

        var total = await cards.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "departmentname" => descending
                ? cards.OrderByDescending(card => card.DepartmentName).ThenByDescending(card => card.ServiceName)
                : cards.OrderBy(card => card.DepartmentName).ThenBy(card => card.ServiceName),
            "fieldcount" => descending
                ? cards.OrderByDescending(card => card.FieldCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.FieldCount).ThenBy(card => card.ServiceName),
            "sectioncount" => descending
                ? cards.OrderByDescending(card => card.SectionCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.SectionCount).ThenBy(card => card.ServiceName),
            _ => descending
                ? cards.OrderByDescending(card => card.ServiceName).ThenByDescending(card => card.ServiceId)
                : cards.OrderBy(card => card.ServiceName).ThenBy(card => card.ServiceId)
        };

        var items = query.PageSize == -1
            ? await ordered.ToListAsync(cancellationToken)
            : await ordered.Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize).ToListAsync(cancellationToken);
        var size = query.PageSize == -1 ? Math.Max(1, total) : query.PageSize;

        return new PagedResult<RTSFieldDefinitionServiceCardDto>(items, total, query.PageNumber, size);
    }

    public async Task<IReadOnlyList<RTSFieldDefinitionDto>> UpdateServiceFormAsync(
        int serviceId,
        UpdateRTSFieldDefinitionFormDto request,
        CancellationToken cancellationToken = default)
    {
        var service = await _services.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            throw new ValidationException("serviceId", "RTS service was not found.", OperationType.Update);

        var sections = request.Sections ?? [];
        var normalizedSections = sections
            .Select(section => new
            {
                Name = (section.Name ?? string.Empty).Trim(),
                Fields = section.Fields ?? []
            })
            .ToList();

        if (normalizedSections.Any(section => string.IsNullOrWhiteSpace(section.Name)))
            throw new ValidationException("sections", "Every section must have a name.", OperationType.Update);

        if (normalizedSections.Any(section => section.Fields.Count == 0))
            throw new ValidationException("sections", "A section must contain at least one field before it can be saved.", OperationType.Update);

        if (normalizedSections.Select(section => section.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedSections.Count)
            throw new ValidationException("sections", "Section names must be unique within a service.", OperationType.Update);

        var submittedFields = normalizedSections
            .SelectMany(section => section.Fields.Select(field => new { section.Name, Field = field }))
            .ToList();

        var duplicateCodes = submittedFields
            .GroupBy(item => (item.Field.FieldCode ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicateCodes is not null)
            throw new ValidationException("fields", "Each field code must be unique within a service.", OperationType.Update);

        ValidateStructuredConfiguration(submittedFields.Select(item => item.Field));

        var submittedIds = submittedFields
            .Where(item => item.Field.Id.HasValue)
            .Select(item => item.Field.Id!.Value)
            .ToList();
        if (submittedIds.Count != submittedIds.Distinct().Count())
            throw new ValidationException("fields", "A configured field can appear only once in the form.", OperationType.Update);

        var existingFields = await _repository.GetQueryable()
            .Where(field => field.ServiceId == serviceId
                && field.DepartmentId == service.DepartmentId
                && !field.MarkedForDeletion)
            .ToListAsync(cancellationToken);
        var existingById = existingFields.ToDictionary(field => field.Id);

        if (submittedIds.Any(id => !existingById.ContainsKey(id)))
            throw new ValidationException("fields", "A field does not belong to this service.", OperationType.Update);

        var fieldsToDelete = existingFields
            .Where(field => !submittedIds.Contains(field.Id))
            .ToList();

        foreach (var field in fieldsToDelete)
        {
            var validation = await ValidateForDeleteAsync(field.Id, field, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(
                    validation.Errors.FirstOrDefault()?.PropertyName ?? "fields",
                    validation.Errors.FirstOrDefault()?.ErrorMessage ?? "The field cannot be deleted because it is in use.",
                    OperationType.Delete);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var section in normalizedSections)
            {
                foreach (var submitted in section.Fields)
                {
                    var entity = submitted.Id.HasValue
                        ? existingById[submitted.Id.Value]
                        : new RTSFieldDefinitionEntity
                        {
                            ServiceId = serviceId,
                            DepartmentId = service.DepartmentId,
                            CreatedBy = request.UpdatedBy
                        };

                    ApplyFormField(entity, submitted, section.Name, submitted.DisplayOrder, request.UpdatedBy);

                    if (submitted.Id.HasValue)
                        await _repository.UpdateAsync(entity, cancellationToken);
                    else
                        await _repository.AddAsync(entity, cancellationToken);
                }
            }

            foreach (var field in fieldsToDelete)
                await _repository.DeleteAsync(field, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var savedFields = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(field => field.ServiceId == serviceId
                    && field.DepartmentId == service.DepartmentId
                    && !field.MarkedForDeletion)
                .OrderBy(field => field.DisplayOrder)
                .ThenBy(field => field.Id)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<RTSFieldDefinitionDto>>(savedFields);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    private static void ApplyFormField(
        RTSFieldDefinitionEntity entity,
        RTSFieldDefinitionFormFieldDto field,
        string sectionName,
        int displayOrder,
        int? updatedBy)
    {
        entity.FieldCode = field.FieldCode.Trim();
        entity.FieldLabel = field.FieldLabel.Trim();
        entity.FieldLabelLocal = string.IsNullOrWhiteSpace(field.FieldLabelLocal) ? null : field.FieldLabelLocal.Trim();
        entity.FieldType = field.FieldType.Trim();
        entity.FieldGroup = sectionName;
        entity.OptionsJson = string.IsNullOrWhiteSpace(field.OptionsJson) ? null : field.OptionsJson;
        entity.IsRequired = field.IsRequired;
        entity.DisplayOrder = displayOrder;
        entity.ValidationRules = string.IsNullOrWhiteSpace(field.ValidationRules) ? null : field.ValidationRules;
        entity.DefaultValue = string.IsNullOrWhiteSpace(field.DefaultValue) ? null : field.DefaultValue;
        entity.MinValue = field.MinValue;
        entity.MaxValue = field.MaxValue;
        entity.MaxLength = field.MaxLength;
        entity.IsActive = field.IsActive;
        entity.UpdatedBy = updatedBy;
    }

    /// <summary>
    /// The form editor stores new configuration as JSON, while existing deployments can still
    /// contain the older pipe/comma-delimited validation strings.  Validate JSON configuration
    /// here without rejecting legacy strings that have not been edited yet.
    /// </summary>
    private static void ValidateStructuredConfiguration(IEnumerable<RTSFieldDefinitionFormFieldDto> fields)
    {
        foreach (var field in fields)
        {
            var fieldType = (field.FieldType ?? string.Empty).Trim().ToLowerInvariant();
            var validation = ParseValidationRules(field);

            if (fieldType is "select" or "dropdown" or "radio")
                ValidateChoiceOptions(field, required: true);
            else if (fieldType == "checkbox" && !string.IsNullOrWhiteSpace(field.OptionsJson))
                ValidateChoiceOptions(field, required: false);

            if (validation is null) continue;

            if (fieldType is "file" or "upload" or "filelatlog" or "file_lat_log")
                ValidateFileRules(validation.Value, field);
        }
    }

    private static JsonElement? ParseValidationRules(RTSFieldDefinitionFormFieldDto field)
    {
        var raw = field.ValidationRules?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Legacy values such as "required|maxLength:10" remain accepted until edited.
        if (!raw.StartsWith('{') && !raw.StartsWith('[')) return null;

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ValidationException("validationRules", $"Validation rules for '{field.FieldCode}' must be a JSON object.", OperationType.Update);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new ValidationException("validationRules", $"Validation rules for '{field.FieldCode}' are not valid JSON.", OperationType.Update);
        }
    }

    private static void ValidateChoiceOptions(RTSFieldDefinitionFormFieldDto field, bool required)
    {
        if (string.IsNullOrWhiteSpace(field.OptionsJson))
        {
            if (required)
                throw new ValidationException("optionsJson", $"Field '{field.FieldCode}' must contain at least one option.", OperationType.Update);
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(field.OptionsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                throw new ValidationException("optionsJson", $"Field '{field.FieldCode}' must contain at least one option.", OperationType.Update);

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in document.RootElement.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.Object
                    || !option.TryGetProperty("value", out var valueElement)
                    || valueElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(valueElement.GetString())
                    || !option.TryGetProperty("label", out var labelElement)
                    || labelElement.ValueKind != JsonValueKind.Object
                    || !HasRequiredLabel(labelElement, "en")
                    || !HasRequiredLabel(labelElement, "hi")
                    || !HasRequiredLabel(labelElement, "mr"))
                {
                    throw new ValidationException("optionsJson", $"Every option for '{field.FieldCode}' needs a value plus English, Hindi, and Marathi labels.", OperationType.Update);
                }

                if (!values.Add(valueElement.GetString()!.Trim()))
                    throw new ValidationException("optionsJson", $"Option values for '{field.FieldCode}' must be unique.", OperationType.Update);
            }
        }
        catch (JsonException)
        {
            throw new ValidationException("optionsJson", $"Options for '{field.FieldCode}' are not valid JSON.", OperationType.Update);
        }
    }

    private static bool HasRequiredLabel(JsonElement labels, string language) =>
        labels.TryGetProperty(language, out var label)
        && label.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(label.GetString());

    private static void ValidateFileRules(JsonElement validation, RTSFieldDefinitionFormFieldDto field)
    {
        var hasFileRule = validation.TryGetProperty("acceptedFormats", out _)
            || validation.TryGetProperty("accept", out _)
            || validation.TryGetProperty("maxFileSizeMb", out _);
        if (!hasFileRule) return;

        if (!validation.TryGetProperty("acceptedFormats", out var formats)
            || formats.ValueKind != JsonValueKind.Array
            || formats.GetArrayLength() == 0
            || formats.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString())))
        {
            throw new ValidationException("validationRules", $"File field '{field.FieldCode}' needs one or more accepted formats.", OperationType.Update);
        }

        if (!validation.TryGetProperty("maxFileSizeMb", out var maxSize)
            || maxSize.ValueKind != JsonValueKind.Number
            || !maxSize.TryGetDecimal(out var size)
            || size <= 0)
        {
            throw new ValidationException("validationRules", $"File field '{field.FieldCode}' needs a positive maximum file size.", OperationType.Update);
        }
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        RTSFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.DepartmentId == entity.DepartmentId
                           && x.ServiceId == entity.ServiceId
                           && x.FieldCode == entity.FieldCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.FieldCode), "RTSFieldDefinition_FieldCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        RTSFieldDefinitionEntity currentEntity,
        RTSFieldDefinitionEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetFieldDefinitionEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        RTSFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetFieldDefinitionEntity>(id, cancellationToken);
    }
}
