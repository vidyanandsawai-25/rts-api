namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Shared data-access contract for verifying the existence of master records.
/// Centralizes the "does this master row exist and is it active?" checks that were previously
/// duplicated inline across property feature flows (Ward, TaxZone, Mouja, Category, etc.).
/// Application services consume these checks to enforce foreign-key business rules; the
/// repository itself contains no business decisions or error messages.
/// </summary>
public interface IMasterRepository
{
    /// <summary>Returns true when an active <c>WardMaster</c> row exists for the given id.</summary>
    Task<bool> WardExistsAsync(int wardId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>TaxZoneMaster</c> row exists for the given id.</summary>
    Task<bool> TaxZoneExistsAsync(int taxZoneId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>MoujaMaster</c> row exists for the given id.</summary>
    Task<bool> MoujaExistsAsync(int moujaId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>PropertyCategoryMaster</c> row exists for the given id.</summary>
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>PropertyTypeMaster</c> row exists for the given id.</summary>
    Task<bool> PropertyTypeExistsAsync(int propertyTypeId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>WingMaster</c> row exists for the given id.</summary>
    Task<bool> WingExistsAsync(int wingId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>FloorMaster</c> row exists for the given id.</summary>
    Task<bool> FloorExistsAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>SubFloorMaster</c> row exists for the given id.</summary>
    Task<bool> SubFloorExistsAsync(int subFloorId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>ConstructionTypeMaster</c> row exists for the given id.</summary>
    Task<bool> ConstructionTypeExistsAsync(int constructionTypeId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>TypeOfUse</c> row exists for the given id.</summary>
    Task<bool> TypeOfUseExistsAsync(int typeOfUseId, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an active <c>SubTypeOfUse</c> row exists for the given id.</summary>
    Task<bool> SubTypeOfUseExistsAsync(int subTypeOfUseId, CancellationToken cancellationToken = default);
}
