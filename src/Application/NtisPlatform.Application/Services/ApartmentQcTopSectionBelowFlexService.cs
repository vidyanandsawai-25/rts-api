using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer service for the ApartmentQC "below flex" status-badge strip.
/// Resolves the property via <see cref="IApartmentQcTopSectionRepository"/> (same identifier
/// precedence as the top-section panel), fetches the workflow-stage and certificate-type status
/// lists via <see cref="IApartmentQcTopSectionBelowFlexRepository"/>, and batch-resolves the
/// CreatedByName/UpdatedByName audit fields.
/// </summary>
public class ApartmentQcTopSectionBelowFlexService : IApartmentQcTopSectionBelowFlexService
{
    private readonly IApartmentQcTopSectionRepository _topSectionRepository;
    private readonly IApartmentQcTopSectionBelowFlexRepository _repository;
    private readonly IUserRepository _userRepository;

    public ApartmentQcTopSectionBelowFlexService(
        IApartmentQcTopSectionRepository topSectionRepository,
        IApartmentQcTopSectionBelowFlexRepository repository,
        IUserRepository userRepository)
    {
        _topSectionRepository = topSectionRepository;
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<ApartmentQcBelowFlexDto?> GetBelowFlexAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default)
    {
        var property = await _topSectionRepository.GetPropertyAsync(query, cancellationToken);
        if (property is null)
        {
            return null;
        }

        var workflowStages = await _repository.GetWorkflowStagesAsync(property.Id, cancellationToken);
        var certificateTypes = await _repository.GetCertificateTypesAsync(property.Id, cancellationToken);

        await ResolveUserNamesAsync(workflowStages, certificateTypes, cancellationToken);

        return new ApartmentQcBelowFlexDto
        {
            PropertyId = property.Id,
            WorkflowStages = workflowStages,
            CertificateTypes = certificateTypes,
        };
    }

    /// <summary>
    /// Batch-resolves every distinct CreatedBy/UpdatedBy id across both lists in a single query,
    /// instead of a per-row lookup (there can be up to 2x the combined stage/certificate-type
    /// count of distinct user ids here).
    /// </summary>
    private async Task ResolveUserNamesAsync(
        List<WorkflowStageStatusDto> workflowStages,
        List<CertificateTypeStatusDto> certificateTypes,
        CancellationToken cancellationToken)
    {
        var userIds = workflowStages.SelectMany(w => new[] { w.CreatedBy, w.UpdatedBy })
            .Concat(certificateTypes.SelectMany(c => new[] { c.CreatedBy, c.UpdatedBy }))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var names = await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.MiddleName, u.LastName })
            .ToDictionaryAsync(
                u => u.Id,
                u => string.Join(" ", new[] { u.FirstName, u.MiddleName, u.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                cancellationToken);

        foreach (var stage in workflowStages)
        {
            if (stage.CreatedBy.HasValue && names.TryGetValue(stage.CreatedBy.Value, out var createdByName))
                stage.CreatedByName = createdByName;
            if (stage.UpdatedBy.HasValue && names.TryGetValue(stage.UpdatedBy.Value, out var updatedByName))
                stage.UpdatedByName = updatedByName;
        }

        foreach (var cert in certificateTypes)
        {
            if (cert.CreatedBy.HasValue && names.TryGetValue(cert.CreatedBy.Value, out var createdByName))
                cert.CreatedByName = createdByName;
            if (cert.UpdatedBy.HasValue && names.TryGetValue(cert.UpdatedBy.Value, out var updatedByName))
                cert.UpdatedByName = updatedByName;
        }
    }
}
