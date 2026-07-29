using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for Assessment stage operations.
/// Handles zone-wise grid data for Assessment workflow stage.
/// </summary>
public interface IAssessmentStageRepository
{
    /// <summary>
    /// Checks whether the requested assessment workflow stage exists.
    /// </summary>
    Task<bool> AssessmentWorkflowStageExistsAsync(int workflowStageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads active assessment status ids from PropertyAssessmentStatusMaster.
    /// </summary>
    Task<Dictionary<string, int>> GetAssessmentStatusIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads active workflow properties with zone/status/renter data.
    /// </summary>
    Task<List<AssessmentStagePropertyProjection>> GetStagePropertiesAsync(
        int workflowStageId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads assessed properties with old mapped values needed for classification.
    /// </summary>
    Task<List<AssessedClassificationPropertyProjection>> GetAssessedClassificationPropertiesAsync(
        int workflowStageId,
        int assessedStatusId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads unassessed properties with zone/type/open-plot data.
    /// </summary>
    Task<List<UnassessedPropertyProjection>> GetUnassessedPropertiesAsync(
        int workflowStageId,
        int unassessedStatusId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads workflow properties classified as Owner or Renter from RenterMast tax liability.
    /// </summary>
    Task<List<RentedClassifiedPropertyProjection>> GetRentedPropertiesAsync(
        int workflowStageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads Rented tab properties with renter flag and all demand values in one set-based query.
    /// </summary>
    Task<List<RentedPropertyDemandProjection>> GetRentedPropertyDemandDataAsync(
        int workflowStageId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads property details and use-type values for classification.
    /// </summary>
    Task<List<AssessmentPropertyUseDetailProjection>> GetPropertyUseDetailsAsync(
        IEnumerable<int> propertyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads property ids that are mixed-use based on PropertyTypeMaster.
    /// </summary>
    Task<List<int>> GetMixedPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads current RV by property from TransMast.
    /// </summary>
    Task<Dictionary<int, decimal>> GetCurrentRvByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates old demand by zone using new-to-old property mapping.
    /// </summary>
    Task<Dictionary<int, decimal>> GetOldDemandByZoneAsync(
        IEnumerable<AssessmentStagePropertyProjection> properties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates current demand by zone from TransMast.
    /// </summary>
    Task<Dictionary<int, decimal>> GetCurrentDemandByZoneAsync(
        IEnumerable<AssessmentStagePropertyProjection> properties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates retro demand by zone from TaxPendingDetailsRetro.
    /// </summary>
    Task<Dictionary<int, decimal>> GetRetroDemandByZoneAsync(
        IEnumerable<AssessmentStagePropertyProjection> properties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates old demand per property using old property mappings.
    /// </summary>
    Task<Dictionary<int, decimal>> GetOldDemandByPropertyAsync(
        IEnumerable<AssessedClassifiedPropertyProjection> properties,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates old demand per new property id using PropertyMapDetail mapping.
    /// </summary>
    Task<Dictionary<int, decimal>> GetOldDemandByPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates current demand per property from TransMast.
    /// </summary>
    Task<Dictionary<int, decimal>> GetCurrentDemandByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates retro demand per property from TaxPendingDetailsRetro.
    /// </summary>
    Task<Dictionary<int, decimal>> GetRetroDemandByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a signing authority id by authority code.
    /// </summary>
    Task<int> GetSignAuthorityIdByCodeAsync(string authorityCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an active property exists.
    /// </summary>
    Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads active property ids from the requested ids.
    /// </summary>
    Task<List<int>> GetExistingPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a property already has an active signature row.
    /// </summary>
    Task<bool> PropertySignatureExistsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads property ids that already have an active signature row.
    /// </summary>
    Task<List<int>> GetExistingPropertySignatureIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts one PropertySignatureDetails row.
    /// </summary>
    Task<int> InsertPropertySignatureAsync(
        int propertyId,
        int userId,
        int signAuthorityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts many PropertySignatureDetails rows.
    /// </summary>
    Task<int> InsertPropertySignaturesAsync(
        IEnumerable<int> propertyIds,
        int userId,
        int signAuthorityId,
        CancellationToken cancellationToken = default);
}
