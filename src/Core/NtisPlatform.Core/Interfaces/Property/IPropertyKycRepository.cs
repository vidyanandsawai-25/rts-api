using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Persistence port for the "Owner and Occupier Registration" use-case on the Property aggregate
/// (KYC / Know Your Customer information: owner, occupier, address, contact, aadhar).
/// <para>
/// Persistence only; business rules live in <c>IPropertyKycService</c> and saving is delegated
/// to <c>IUnitOfWork</c>. Extends <see cref="IPropertyAggregateRepository"/> — the shared
/// aggregate-root load is inherited, not repeated.
/// </para>
/// </summary>
public interface IPropertyKycRepository : IPropertyAggregateRepository
{
    /// <summary>Reads the KYC projection (owner/occupier, address, contact + resolved owner-type), or null when not found.</summary>
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Id of the first active, non-deleted assessment row for the property, or 0 when none exists.</summary>
    Task<int> GetFirstAssessmentIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked assessment row by id, or null.</summary>
    Task<PropertyAssessmentEntity?> GetAssessmentByIdAsync(int assessmentId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new assessment row for insertion (persisted later via the unit of work).</summary>
    Task AddAssessmentAsync(PropertyAssessmentEntity assessment, CancellationToken cancellationToken = default);
}
