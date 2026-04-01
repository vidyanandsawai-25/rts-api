using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services;

/// <summary>
/// ULB (Urban Local Body) configuration service implementation
/// Provides organization configuration details
/// </summary>
public class UlbConfigService : IUlbConfigService
{
    private readonly IRepository<ULBMasterEntity> _ulbRepository;

    public UlbConfigService(IRepository<ULBMasterEntity> ulbRepository)
    {
        _ulbRepository = ulbRepository;
    }

    /// <summary>
    /// Get ULB configuration from database
    /// Fetches the first active ULB record (ordered by UlbId for consistency)
    /// </summary>
    public async Task<UlbConfigDto?> GetUlbConfigAsync(CancellationToken cancellationToken = default)
    {
        // Use direct query for efficiency - fetch only one row instead of loading all active ULBs
        // Explicit ordering ensures deterministic results when multiple active ULBs exist
        var ulb = await _ulbRepository.GetQueryable()
            .Where(u => u.IsActive)
            .OrderBy(u => u.UlbId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ulb == null)
        {
            return null;
        }

        return new UlbConfigDto
        {
            UlbId = ulb.UlbId,
            UlbCode = ulb.UlbCode,
            UlbName = ulb.UlbName,
            UlbNameLocal = ulb.UlbNameLocal,
            UlbLogo = ulb.UlbLogo,
            EmailId = ulb.EmailId,
            MobileNo = ulb.MobileNo,
            WebsiteUrl = ulb.WebsiteUrl,
            UlbAddress = ulb.UlbAddress,
            State = ulb.State,
            District = ulb.District
        };
    }
}
