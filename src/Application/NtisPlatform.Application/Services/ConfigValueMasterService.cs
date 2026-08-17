using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ConfigValueMaster CRUD operations. Writes invalidate the caches of every service
/// backed by these config tables (SECURITY_AUTH thresholds, EmailSettings SMTP config, ...) so
/// changes take effect immediately instead of waiting for the cache to expire on its own.
/// </summary>
public class ConfigValueMasterService : BaseCommonCrudService<ConfigValueMasterEntity, ConfigValueMasterDto, CreateConfigValueMasterDto, UpdateConfigValueMasterDto, ConfigValueMasterQueryParameters, int>, IConfigValueMasterService
{
    private readonly ISecuritySettingsService _securitySettings;
    private readonly IEmailSettingsProvider _emailSettings;

    public ConfigValueMasterService(
        IRepository<ConfigValueMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISecuritySettingsService securitySettings,
        IEmailSettingsProvider emailSettings)
        : base(repository, unitOfWork, mapper)
    {
        _securitySettings = securitySettings;
        _emailSettings = emailSettings;
    }

    private async Task RefreshConfigCachesAsync(CancellationToken cancellationToken)
    {
        await _securitySettings.RefreshCacheAsync(cancellationToken);
        await _emailSettings.RefreshCacheAsync(cancellationToken);
    }

    public override async Task<ConfigValueMasterDto> CreateAsync(CreateConfigValueMasterDto createDto, CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(createDto, cancellationToken);
        await RefreshConfigCachesAsync(cancellationToken);
        return result;
    }

    public override async Task<ConfigValueMasterDto?> UpdateAsync(int id, UpdateConfigValueMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        var result = await base.UpdateAsync(id, updateDto, cancellationToken);
        await RefreshConfigCachesAsync(cancellationToken);
        return result;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await base.DeleteAsync(id, cancellationToken);
        await RefreshConfigCachesAsync(cancellationToken);
        return result;
    }
}
