using NtisPlatform.Application.DTOs.UserScreenAccess;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for managing user screen access operations
/// </summary>
public interface IUserScreenAccessService
{
    /// <summary>
    /// Get user screen access with filtering and pagination
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of user screen access data</returns>
    Task<PagedResult<UserScreenAccessDto>> GetUserScreenAccessAsync(
        UserScreenAccessQueryParameters queryParams, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all screens accessible to a specific user
    /// </summary>
    /// <param name="userId">The user ID to get screen access for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of screens accessible to the user</returns>
    Task<IEnumerable<UserScreenAccessDto>> GetUserScreensByUserIdAsync(
        int userId, 
        CancellationToken cancellationToken = default);
}
