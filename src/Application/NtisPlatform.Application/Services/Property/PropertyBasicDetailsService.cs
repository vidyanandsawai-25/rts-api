using NtisPlatform.Application.Exceptions;
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

    public PropertyBasicDetailsService(
        IPropertyBasicDetailsRepository repository,
        IMasterRepository masterRepository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy)
    {
        _repository = repository;
        _masterRepository = masterRepository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
    }

    public Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetBasicDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Load the property. A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Step 2: Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Step 3: Validate foreign keys (business rule). Messages preserved for the API contract.
        await ValidateForeignKeysAsync(dto, cancellationToken);

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
            await UpsertSocietyAsync(property, propertyId, dto, now, cancellationToken);

            // Step 7: Final save — persists all property, assessment and plot changes.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Step 8: Return updated data via the read path (AsNoTracking projection).
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
        bool hasAssessmentData = dto.NoOfResidentialToilets.HasValue || dto.NoOfCommercialToilets.HasValue;

        if (assessmentId > 0)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId, cancellationToken);
            if (assessment != null)
            {
                assessment.NoOfResidentialToilets = dto.NoOfResidentialToilets;
                assessment.NoOfCommercialToilets = dto.NoOfCommercialToilets;
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
    /// Search order: (1) by the FK on the parent, (2) by PropertyId (prevents duplicates in legacy data),
    /// (3) create new when wing data is present. An intermediate save is performed only when creating a
    /// new row so that the generated PK is available to link back to the property. Both that save and the
    /// caller's final save are wrapped in the same transaction by <see cref="UpdateBasicDetailsAsync"/>.
    /// </summary>
    private async Task UpsertSocietyAsync(
        PropertyEntity property,
        int propertyId,
        UpdatePropertyBasicDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        SocietyDetailsEntity? society = null;

        // Step 1: try by the FK stored on the parent.
        if (property.SocietyDetailId.HasValue)
            society = await _repository.GetSocietyByIdAsync(property.SocietyDetailId.Value, cancellationToken);

        // Step 2: FK was null/stale — fall back to lookup by PropertyId to prevent duplicate rows.
        if (society == null)
        {
            society = await _repository.GetSocietyByPropertyIdAsync(propertyId, cancellationToken);
            if (society != null && property.SocietyDetailId != society.Id)
                property.SocietyDetailId = society.Id;
        }

        // Step 3: create new society if still not found and any wing data is being set.
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
            property.SocietyDetailId = society.Id;
        }

        if (society != null)
        {
            society.WingId = dto.WingId;
            society.WingName = dto.WingName;

            if (dto.WingNo != null)
            {
                var wing = await _repository.GetActiveWingByNoAsync(dto.WingNo, cancellationToken);
                if (wing != null)
                    society.WingId = wing.Id;
            }

            society.UpdatedDate = now;
        }
    }
}
