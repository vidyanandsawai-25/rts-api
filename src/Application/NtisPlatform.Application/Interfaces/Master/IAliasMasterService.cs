using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;

using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// CRUD for the <c>CORE.AliasMaster</c> master — per-language display aliases (English/Regional/
/// Hindi) for software field names. Every write is immediate and live; there is no draft/approval
/// workflow.
/// </summary>
public interface IAliasMasterService : ICommonCrudService<AliasMasterEntity, AliasMasterDto, CreateAliasMasterDto, UpdateAliasMasterDto, AliasMasterQueryParameters, int>
{
    /// <summary>Returns false if no row with <paramref name="id"/> exists.</summary>
    Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// All active rows' per-language names, keyed by <c>KeyName</c> on the client — used to
    /// override the app's static JSON translations wherever a screen opts in via that field name.
    /// </summary>
    Task<List<AliasLabelDto>> GetActiveAliasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate counts: total fields, active fields, and inactive fields in Alias Master.
    /// </summary>
    Task<AliasMasterCountDto> GetCountsAsync(CancellationToken cancellationToken = default);
}

