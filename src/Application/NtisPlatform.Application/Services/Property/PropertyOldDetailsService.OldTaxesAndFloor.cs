using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Old taxes and old (historical) floor sub-sections of the Property "Old Details" use case.
/// Validation and the old-taxes write transaction are encapsulated in the feature repository; this
/// service is the use-case boundary the controller depends on and performs the paged-result mapping.
/// </summary>
public partial class PropertyOldDetailsService
{
    // ---- Old Taxes Details sub-section ----

    public Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetOldTaxesDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyOldTaxesDetailsDto?> CreateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        await ValidateOldTaxesRequestAsync(dto, cancellationToken);

        // Create-only: reject if any requested year-tax combination already has an active record.
        if (property.PropertyMastOldId.HasValue)
        {
            await EnsureNoOldTaxConflictsAsync(property.PropertyMastOldId.Value, dto, cancellationToken);
        }

        return await _repository.PersistNewOldTaxesAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        await ValidateOldTaxesRequestAsync(dto, cancellationToken);

        // Upsert: existing year-tax rows (including soft-deleted) are updated/reactivated, so no conflict check.
        return await _repository.PersistUpsertedOldTaxesAsync(propertyId, dto, cancellationToken);
    }

    /// <summary>
    /// Shared old-taxes business validation: no duplicate years, all years exist and are not in the future,
    /// all taxes are configured for old taxes, and no duplicate tax within a single year. Messages preserved for the API contract.
    /// </summary>
    private async Task ValidateOldTaxesRequestAsync(UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken)
    {
        var requestedFinanceYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).ToList();
        var financeYearIds = requestedFinanceYearIds.Distinct().ToList();

        if (requestedFinanceYearIds.Count != financeYearIds.Count)
        {
            throw new PropertyValidationException("Duplicate finance years are not allowed in the request");
        }

        var currentYear = DateTime.Now.Year;
        var years = await _repository.GetYearsByIdsAsync(financeYearIds, cancellationToken);

        if (years.Count != financeYearIds.Count)
        {
            throw new PropertyValidationException("One or more finance years are invalid");
        }

        var invalidYears = years.Where(y => y.Value > currentYear).Select(y => y.Value).ToList();
        if (invalidYears.Any())
        {
            throw new PropertyValidationException(
                $"Year cannot be greater than the current year ({currentYear}). Invalid year(s): {string.Join(", ", invalidYears)}");
        }

        var allTaxIds = dto.TaxYears
            .SelectMany(ty => ty.Taxes.Select(t => t.TaxId))
            .Distinct()
            .ToList();

        var validTaxIds = await _repository.GetValidOldTaxIdsAsync(allTaxIds, cancellationToken);
        if (validTaxIds.Count != allTaxIds.Count)
        {
            throw new PropertyValidationException("One or more tax types are invalid or not configured for old taxes");
        }

        foreach (var yearDto in dto.TaxYears)
        {
            var duplicateTaxIds = yearDto.Taxes
                .GroupBy(t => t.TaxId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTaxIds.Any())
            {
                throw new PropertyValidationException(
                    $"Duplicate TaxId(s) found for year {yearDto.FinanceYearId}: {string.Join(", ", duplicateTaxIds)}. " +
                    "Each tax can only appear once per finance year.");
            }
        }
    }

    /// <summary>Rejects a create when any requested year-tax combination already has an active record.</summary>
    private async Task EnsureNoOldTaxConflictsAsync(int propertyMastOldId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken)
    {
        var requestedYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).Distinct().ToList();
        var requestedCombinations = dto.TaxYears
            .SelectMany(ty => ty.Taxes.Select(t => (YearId: ty.FinanceYearId, TaxId: t.TaxId)))
            .ToList();

        var existingKeys = await _repository.GetActiveOldTaxKeysAsync(propertyMastOldId, requestedYearIds, cancellationToken);

        var conflicts = requestedCombinations
            .Where(req => existingKeys.Contains((req.YearId, req.TaxId)))
            .ToList();

        if (!conflicts.Any())
            return;

        var yearCodes = await _repository.GetYearCodeMapAsync(conflicts.Select(c => c.YearId).Distinct().ToList(), cancellationToken);
        var taxNames = await _repository.GetTaxNameMapAsync(conflicts.Select(c => c.TaxId).Distinct().ToList(), cancellationToken);

        var conflictDetails = conflicts
            .Select(c =>
            {
                var year = yearCodes.TryGetValue(c.YearId, out var yc) && yc != null ? yc : c.YearId.ToString();
                var tax = taxNames.TryGetValue(c.TaxId, out var tn) ? tn : c.TaxId.ToString();
                return $"{year} - {tax}";
            })
            .ToList();

        throw new PropertyValidationException(
            $"Cannot create records - the following year-tax combinations already exist: {string.Join(", ", conflictDetails)}. " +
            "Use PUT endpoint to update existing records.");
    }

    // ---- Old Floor Details sub-section ----

    public Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetFloorDetailsOldAsync(propertyId, cancellationToken);

    public async Task<PagedResult<PropertyDetailsOldDto>?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        // Map the API query parameters to the data-access query model.
        var query = new FloorDetailsOldQuery
        {
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            SearchTerm = queryParameters.SearchTerm,
            SortBy = queryParameters.SortBy,
            SortOrder = queryParameters.SortOrder,
            OldFloorId = queryParameters.OldFloorId,
            OldSubFloorId = queryParameters.OldSubFloorId,
            OldConstructionTypeId = queryParameters.OldConstructionTypeId,
            OldTypeOfUseId = queryParameters.OldTypeOfUseId,
            OldSubTypeOfUseId = queryParameters.OldSubTypeOfUseId,
            OldConstructionYear = queryParameters.OldConstructionYear,
            OldAssessmentYear = queryParameters.OldAssessmentYear
        };

        var result = await _repository.GetFloorDetailsOldPagedAsync(propertyId, query, cancellationToken);

        if (result == null)
            return null;

        return new PagedResult<PropertyDetailsOldDto>(
            result.Items,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);
    }

    public Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
        => _repository.GetFloorDetailsOldByIdAsync(propertyId, floorId, cancellationToken);

    public async Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        // Mutation paths must NOT diverge: floor inserts go through the same boundary as old-taxes/old-details.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        await ValidateFloorReferencesAsync(dto.OldFloorId, dto.OldSubFloorId, dto.OldConstructionTypeId, dto.OldTypeOfUseId, dto.OldSubTypeOfUseId, cancellationToken);

        return await _repository.AddFloorDetailsOldAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        if (!property.PropertyMastOldId.HasValue)
            throw new PropertyValidationException($"Property {propertyId} does not have an associated PropertyMastOld record");

        await ValidateFloorReferencesAsync(dto.OldFloorId, dto.OldSubFloorId, dto.OldConstructionTypeId, dto.OldTypeOfUseId, dto.OldSubTypeOfUseId, cancellationToken);

        return await _repository.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, cancellationToken);
    }

    /// <summary>
    /// Validates the optional floor/sub-floor/construction-type/type-of-use/sub-type-of-use references against the
    /// master data. Messages and order preserved for the API contract.
    /// </summary>
    private async Task ValidateFloorReferencesAsync(int? oldFloorId, int? oldSubFloorId, int? oldConstructionTypeId, int? oldTypeOfUseId, int? oldSubTypeOfUseId, CancellationToken cancellationToken)
    {
        if (oldFloorId.HasValue && !await _masterRepository.FloorExistsAsync(oldFloorId.Value, cancellationToken))
            throw new PropertyValidationException($"Invalid or inactive Floor ID: {oldFloorId.Value}");

        if (oldSubFloorId.HasValue && !await _masterRepository.SubFloorExistsAsync(oldSubFloorId.Value, cancellationToken))
            throw new PropertyValidationException($"Invalid or inactive SubFloor ID: {oldSubFloorId.Value}");

        if (oldConstructionTypeId.HasValue && !await _masterRepository.ConstructionTypeExistsAsync(oldConstructionTypeId.Value, cancellationToken))
            throw new PropertyValidationException($"Invalid or inactive ConstructionType ID: {oldConstructionTypeId.Value}");

        if (oldTypeOfUseId.HasValue && !await _masterRepository.TypeOfUseExistsAsync(oldTypeOfUseId.Value, cancellationToken))
            throw new PropertyValidationException($"Invalid or inactive TypeOfUse ID: {oldTypeOfUseId.Value}");

        if (oldSubTypeOfUseId.HasValue && !await _masterRepository.SubTypeOfUseExistsAsync(oldSubTypeOfUseId.Value, cancellationToken))
            throw new PropertyValidationException($"Invalid or inactive SubTypeOfUse ID: {oldSubTypeOfUseId.Value}");
    }

    public Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
        => _repository.DeleteFloorDetailsOldAsync(propertyId, floorId, cancellationToken);
}
