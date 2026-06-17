using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Persistence port for the "Record Identification and Classification" use-case on the Property aggregate.
/// Owns only persistence concerns: reading the basic-details projection and loading/adding the related
/// rows (assessment, plot, society, wing) that this use-case writes.
/// <para>
/// Business rules (existence checks, foreign-key validation, upsert decisions, null-update rules and
/// exception messages) live in <c>IPropertyBasicDetailsService</c>. Saving is delegated to
/// <c>IUnitOfWork</c> by the service so this repository never calls SaveChanges.
/// </para>
/// <para>
/// Extends <see cref="IPropertyAggregateRepository"/> — <see cref="IPropertyAggregateRepository.GetActivePropertyAsync"/>
/// is declared once on the base interface and inherited here, eliminating the duplicated signature that
/// previously appeared across every per-tab repository port.
/// </para>
/// </summary>
public interface IPropertyBasicDetailsRepository : IPropertyAggregateRepository
{
    /// <summary>
    /// Reads the composed Basic Details projection (property + ward/zone/tax-zone/category/type/mouja
    /// joins, assessment toilet counts, summed areas, first plot and resolved wing) for a property.
    /// Returns null when the property does not exist, is inactive, or is marked for deletion.
    /// </summary>
    Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of the first active, non-deleted assessment row for the property, or 0 when none exists.</summary>
    Task<int> GetFirstAssessmentIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked assessment row by id, or null when not found.</summary>
    Task<PropertyAssessmentEntity?> GetAssessmentByIdAsync(int assessmentId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new assessment row for insertion (persisted later via the unit of work).</summary>
    Task AddAssessmentAsync(PropertyAssessmentEntity assessment, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of the first active plot row for the property, or 0 when none exists.</summary>
    Task<int> GetFirstPlotIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked plot row by id, or null when not found.</summary>
    Task<PlotDetailsEntity?> GetPlotByIdAsync(int plotId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new plot row for insertion (persisted later via the unit of work).</summary>
    Task AddPlotAsync(PlotDetailsEntity plot, CancellationToken cancellationToken = default);

    /// <summary>Loads the active, non-deleted society row by its id as a tracked entity, or null when not found.</summary>
    Task<SocietyDetailsEntity?> GetSocietyByIdAsync(int societyId, CancellationToken cancellationToken = default);

    /// <summary>Loads the active, non-deleted society row for a property as a tracked entity, or null when not found.</summary>
    Task<SocietyDetailsEntity?> GetSocietyByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new society row for insertion (persisted later via the unit of work).</summary>
    void AddSociety(SocietyDetailsEntity society);

    /// <summary>Resolves an active wing by its <c>WingNo</c>, or null when not found.</summary>
    Task<WingEntity?> GetActiveWingByNoAsync(string wingNo, CancellationToken cancellationToken = default);
}
