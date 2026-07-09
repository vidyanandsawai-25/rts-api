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
    private readonly IRepository<UlbImageMasterEntity> _ulbImageMasterRepository;
    private readonly IRepository<DocumentEntity> _documentRepository;

    public UlbConfigService(
        IRepository<ULBMasterEntity> ulbRepository,
        IRepository<UlbImageMasterEntity> ulbImageMasterRepository,
        IRepository<DocumentEntity> documentRepository)
    {
        _ulbRepository = ulbRepository;
        _ulbImageMasterRepository = ulbImageMasterRepository;
        _documentRepository = documentRepository;
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
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (ulb == null)
        {
            return null;
        }

        var backgroundImageGuid = await (
            from img in _ulbImageMasterRepository.GetQueryable()
            where img.ImageType == "Background" && img.IsActive
            join doc in _documentRepository.GetQueryable() on img.ImageId equals doc.Id
            where doc.IsActive && !doc.MarkedForDeletion
            orderby img.Id descending
            select doc.DocumentGuid
        ).FirstOrDefaultAsync(cancellationToken);

        string? ulbBackground = null;
        if (backgroundImageGuid != Guid.Empty)
        {
            ulbBackground = $"/api/UlbImageMaster/{backgroundImageGuid}/view";
        }

        return new UlbConfigDto
        {
            UlbId = ulb.Id,
            UlbCode = ulb.UlbCode,
            UlbName = ulb.UlbName,
            UlbNameLocal = ulb.UlbNameLocal,
            UlbLogo = ulb.UlbLogo,
            EmailId = ulb.EmailId,
            MobileNo = ulb.MobileNo,
            WebsiteUrl = ulb.WebsiteUrl,
            UlbAddress = ulb.UlbAddress,
            State = ulb.State,
            District = ulb.District,
            UlbBackground = ulbBackground
        };
    }
}
