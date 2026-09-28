using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Record Identification and Classification" use-case for the Property aggregate.
/// Owns: aggregate-invariant enforcement, FK validation, the upsert decisions for assessment/plot/society
/// child rows, a single transaction boundary that protects the mid-operation society-save from leaving
/// the parent un-linked on failure, and a consistent timestamp across all entity fields.
/// Persistence is delegated to <see cref="IPropertyBasicDetailsRepository"/>, master checks to
/// <see cref="IMasterRepository"/>, saving / transactions to <see cref="IUnitOfWork"/>, and
/// aggregate invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public class PropertyBasicDetailsService : IPropertyBasicDetailsService
{
    private readonly IPropertyBasicDetailsRepository _repository;
    private readonly IMasterRepository _masterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;
    private readonly ITaxZoningRangeService _taxZoningRangeService;
    private readonly ICurrentUserService _currentUserService;

    public PropertyBasicDetailsService(
        IPropertyBasicDetailsRepository repository,
        IMasterRepository masterRepository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy,
        ITaxZoningRangeService taxZoningRangeService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _masterRepository = masterRepository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
        _taxZoningRangeService = taxZoningRangeService;
        _currentUserService = currentUserService;
    }

    public Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetBasicDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId();

        // Step 1: Load the property. A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Step 2: Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Step 3: Validate foreign keys (business rule). Messages preserved for the API contract.
        await ValidateForeignKeysAsync(dto, cancellationToken);

        // Capture old zone before any field is overwritten — needed for reconciliation below.
        var oldTaxZoneId = property.TaxZoneId;
        var propertyNo = property.PropertyNo ?? string.Empty;
        var wardId = property.WardId;

        // Single timestamp: every entity field in this operation uses the same value.
        var now = DateTime.Now;

        // The society upsert may perform an intermediate save (to get the generated PK) before
        // the final save below. Wrapping the entire operation in a transaction guarantees that a
        // failure at any point rolls back ALL changes atomically — no orphaned child rows.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 3: Update PropertyMast fields — extracted helper keeps the method readable.
            ApplyPropertyFields(property, dto, now);

            // Step 4 & 5: Upsert assessment (toilet counts) and plot dimensions.
            await UpsertAssessmentAsync(propertyId, dto, now, cancellationToken);
            await UpsertPlotAsync(propertyId, dto, now, cancellationToken);

            // Step 6: Upsert society for WingId / WingNo / WingName.
            await UpsertSocietyAsync(propertyId, dto, now, cancellationToken);

            // Step 7: Final save — persists all property, assessment and plot changes.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Step 8: If the tax zone changed, reconcile TaxZoningRange — trim/split the old range
        // and create a new single-property range for the new zone. Runs outside the main
        // transaction so a reconciliation failure doesn't roll back the property save.
        if (dto.TaxZoneId != oldTaxZoneId && !string.IsNullOrWhiteSpace(propertyNo))
        {
            try
            {
                await _taxZoningRangeService.ReconcilePropertyZoneChangeAsync(
                    propertyId, wardId, propertyNo, dto.TaxZoneId, userId, cancellationToken);
            }
            catch
            {
                // Gracefully catch and swallow any reconciliation exceptions so a bookkeeping 
                // failure does not rollback or fail the successful property details save.
            }
        }

        // Step 9: Return updated data via the read path (AsNoTracking projection).
        return await _repository.GetBasicDetailsAsync(propertyId, cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies all editable PropertyMast fields from the DTO in a single place.
    /// Using one <paramref name="now"/> value ensures a consistent timestamp.
    /// </summary>
    private static void ApplyPropertyFields(
        PropertyEntity property,
        UpdatePropertyBasicDetailsDto dto,
        DateTime now)
    {
        property.WardId = dto.WardId;
        property.TaxZoneId = dto.TaxZoneId;
        property.CategoryId = dto.CategoryId;
        property.PropertyTypeId = dto.PropertyTypeId;
        property.PartitionNo = dto.PartitionNo;
        property.FlatOrShopNo = dto.FlatOrShopNo;
        property.PlotNo = dto.PlotNo;
        property.CSN = dto.SurveyNo;
        property.UPICId = dto.UPICId;
        property.SubZoneNo = dto.SubZoneNo;
        property.MoujaId = dto.MoujaId;
        property.UpdatedDate = now;
    }

    /// <summary>
    /// Enforces the foreign-key business rules. Throws <see cref="InvalidOperationException"/> with the
    /// exact messages the API translates into a 400 response. Order (TaxZone, Ward, Mouja) is preserved.
    /// </summary>
    private async Task ValidateForeignKeysAsync(UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken)
    {
        if (!await _masterRepository.TaxZoneExistsAsync(dto.TaxZoneId, cancellationToken))
            throw new PropertyValidationException($"TaxZone with ID {dto.TaxZoneId} does not exist or is inactive.");

        if (!await _masterRepository.WardExistsAsync(dto.WardId, cancellationToken))
            throw new PropertyValidationException($"Ward with ID {dto.WardId} does not exist or is inactive.");

        if (dto.MoujaId.HasValue && !await _masterRepository.MoujaExistsAsync(dto.MoujaId.Value, cancellationToken))
            throw new PropertyValidationException($"Mouja with ID {dto.MoujaId.Value} does not exist or is inactive.");
    }

    /// <summary>
    /// Upserts the assessment row (toilet counts). Updates the existing row in place (even with nulls),
    /// or inserts a new row only when toilet data is supplied.
    /// </summary>
    private async Task UpsertAssessmentAsync(
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var assessmentId = await _repository.GetFirstAssessmentIdAsync(propertyId, cancellationToken);
        bool hasAssessmentData = dto.NoOfResidentialToilets.HasValue ||
                                 dto.NoOfCommercialToilets.HasValue ||
                                 !string.IsNullOrWhiteSpace(dto.Latitude) ||
                                 !string.IsNullOrWhiteSpace(dto.Longitude);

        if (assessmentId > 0)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId, cancellationToken);
            if (assessment != null)
            {
                assessment.NoOfResidentialToilets = dto.NoOfResidentialToilets;
                assessment.NoOfCommercialToilets = dto.NoOfCommercialToilets;
                assessment.Latitude = dto.Latitude;
                assessment.Longitude = dto.Longitude;
                assessment.UpdatedDate = now;
            }
        }
        else if (hasAssessmentData)
        {
            await _repository.AddAssessmentAsync(new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                NoOfResidentialToilets = dto.NoOfResidentialToilets,
                NoOfCommercialToilets = dto.NoOfCommercialToilets,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = now
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Upserts the plot row. Updates the existing row in place (even with nulls), or inserts a new row
    /// only when plot data is supplied.
    /// </summary>
    private async Task UpsertPlotAsync(
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var plotId = await _repository.GetFirstPlotIdAsync(propertyId, cancellationToken);
        bool hasPlotData = dto.PlotArea.HasValue || dto.PlotAreaFtLength.HasValue ||
                           dto.PlotAreaFtWidth.HasValue || dto.PlotAreaMtrLength.HasValue ||
                           dto.PlotAreaMtrWidth.HasValue;

        if (plotId > 0)
        {
            var plot = await _repository.GetPlotByIdAsync(plotId, cancellationToken);
            if (plot != null)
            {
                plot.PlotArea = dto.PlotArea;
                plot.PlotAreaFtLength = dto.PlotAreaFtLength;
                plot.PlotAreaFtWidth = dto.PlotAreaFtWidth;
                plot.PlotAreaMtrLength = dto.PlotAreaMtrLength;
                plot.PlotAreaMtrWidth = dto.PlotAreaMtrWidth;
                plot.UpdatedDate = now;
            }
        }
        else if (hasPlotData)
        {
            await _repository.AddPlotAsync(new PlotDetailsEntity
            {
                PropertyId = propertyId,
                PlotArea = dto.PlotArea,
                PlotAreaFtLength = dto.PlotAreaFtLength,
                PlotAreaFtWidth = dto.PlotAreaFtWidth,
                PlotAreaMtrLength = dto.PlotAreaMtrLength,
                PlotAreaMtrWidth = dto.PlotAreaMtrWidth,
                IsActive = true,
                CreatedDate = now
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Upserts the society row that stores WingId / WingName, resolving WingNo to a wing when supplied.
    /// Looked up by PropertyId (the reverse FK) — PropertyMast no longer carries a forward SocietyDetailId
    /// FK. Creates a new row when not found and any wing data is present. An intermediate save is performed
    /// only when creating a new row so that the generated PK is available to link back to the property. Both
    /// that save and the caller's final save are wrapped in the same transaction by <see cref="UpdateBasicDetailsAsync"/>.
    /// </summary>
    private async Task UpsertSocietyAsync(
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var society = await _repository.GetSocietyByPropertyIdAsync(propertyId, cancellationToken);

        // create new society if still not found and any wing data is being set.
        if (society == null && (dto.WingId.HasValue || dto.WingName != null || dto.WingNo != null))
        {
            society = new SocietyDetailsEntity
            {
                PropertyId = propertyId,
                IsActive = true,
                CreatedDate = now
            };
            _repository.AddSociety(society);

            // Flush to get the generated PK; caller's transaction ensures this is rolled back
            // together with any later failure — the parent is never left un-linked.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (society != null)
        {
            society.UpdatedDate = now;

            int? targetWingId = dto.WingId;
            if (!targetWingId.HasValue && !string.IsNullOrWhiteSpace(dto.WingNo))
            {
                var wingObj = await _repository.GetActiveWingByNoAsync(dto.WingNo.Trim(), cancellationToken);
                if (wingObj != null)
                {
                    targetWingId = wingObj.Id;
                }
            }

            if (targetWingId.HasValue || !string.IsNullOrWhiteSpace(dto.WingNo) || !string.IsNullOrWhiteSpace(dto.WingName))
            {
                var wingMast = await _repository.GetWingDetailsMastBySocietyIdAsync(society.Id, cancellationToken);
                var wingName = !string.IsNullOrWhiteSpace(dto.WingName) ? dto.WingName : dto.WingNo;
                if (wingMast != null)
                {
                    if (targetWingId.HasValue) wingMast.WingMasterId = targetWingId.Value;
                    if (!string.IsNullOrWhiteSpace(wingName)) wingMast.WingName = wingName;
                    wingMast.UpdatedDate = now;
                }
                else if (targetWingId.HasValue)
                {
                    // WingMasterId is a required FK -- only create a new row when we actually
                    // have a valid master id. WingNo/WingName-only with no matching WingEntity
                    // and no existing row has nothing sensible to link to, so it's a no-op
                    // rather than writing WingMasterId=0.
                    wingMast = new WingDetailsMastEntity
                    {
                        SocietyDetailsMastId = society.Id,
                        WingMasterId = targetWingId.Value,
                        WingName = wingName,
                        IsActive = true,
                        CreatedDate = now
                    };
                    _repository.AddWingDetailsMast(wingMast);
                }
            }
        }
    }
}
