using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ConfigKeyMaster CRUD operations. Writes invalidate the caches of every service
/// backed by these config tables (SECURITY_AUTH thresholds, EmailSettings SMTP config, ...) so
/// changes (e.g. a key's DefaultValue, or deactivating a key) take effect immediately instead of
/// waiting for the cache to expire on its own.
/// </summary>
public class ConfigKeyMasterService : BaseCommonCrudService<ConfigKeyMasterEntity, ConfigKeyMasterDto, CreateConfigKeyMasterDto, UpdateConfigKeyMasterDto, ConfigKeyMasterQueryParameters, int>, IConfigKeyMasterService
{
    private readonly ISecuritySettingsService _securitySettings;
    private readonly IEmailSettingsProvider _emailSettings;

    public ConfigKeyMasterService(
        IRepository<ConfigKeyMasterEntity, int> repository,
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

    public override async Task<ConfigKeyMasterDto> CreateAsync(CreateConfigKeyMasterDto createDto, CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(createDto, cancellationToken);
        await RefreshConfigCachesAsync(cancellationToken);
        return result;
    }

    public override async Task<ConfigKeyMasterDto?> UpdateAsync(int id, UpdateConfigKeyMasterDto updateDto, CancellationToken cancellationToken = default)
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
