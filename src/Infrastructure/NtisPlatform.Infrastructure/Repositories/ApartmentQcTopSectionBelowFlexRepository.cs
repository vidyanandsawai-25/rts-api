using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for the ApartmentQC "below flex" status-badge strip.
/// Every query is AsNoTracking — this feature never writes.
/// </summary>
public sealed class ApartmentQcTopSectionBelowFlexRepository : IApartmentQcTopSectionBelowFlexRepository
{
    private readonly ApplicationDbContext _context;

    public ApartmentQcTopSectionBelowFlexRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkflowStageStatusDto>> GetWorkflowStagesAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var stages = await _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new { s.Id, s.StageName, s.Description })
            .ToListAsync(cancellationToken);

        if (stages.Count == 0)
            return new List<WorkflowStageStatusDto>();

        var stageIds = stages.Select(s => s.Id).ToList();

        var details = await _context.PropertyWorkflowDetails
            .AsNoTracking()
            .Where(d => d.PropertyId == propertyId && stageIds.Contains(d.WorkflowStageId) && d.IsActive)
            .ToListAsync(cancellationToken);

        // Latest row per stage (a stage can accumulate multiple PropertyWorkflowDetails rows for
        // the same property over time) represents the current state — same "distinct completed
        // stage" semantics as GetWorkflowCompletionAsync/GetWorkflowStageDetailsAsync elsewhere.
        var latestByStage = details
            .GroupBy(d => d.WorkflowStageId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(d => d.CreatedDate ?? DateTime.MinValue).ThenByDescending(d => d.Id).First());

        return stages.Select(stage =>
        {
            latestByStage.TryGetValue(stage.Id, out var latest);
            return new WorkflowStageStatusDto
            {
                StageId = stage.Id,
                StageName = stage.StageName,
                Description = stage.Description,
                IsCompleted = latest?.CurrentStatus == true,
                CreatedBy = latest?.CreatedBy,
                CreatedDate = latest?.CreatedDate,
                UpdatedBy = latest?.UpdatedBy,
                UpdatedDate = latest?.UpdatedDate,
            };
        }).ToList();
    }

    public async Task<List<CertificateTypeStatusDto>> GetCertificateTypesAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var types = await _context.PropertyCertificateTypeMasters
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.CertificateTypeCode, t.CertificateTypeName, t.Description })
            .ToListAsync(cancellationToken);

        if (types.Count == 0)
            return new List<CertificateTypeStatusDto>();

        var typeIds = types.Select(t => t.Id).ToList();

        // Property-level certificates (EntityType == "P", PropertyDetailsId == null)
        var certs = await _context.PropertyCertificates
            .AsNoTracking()
            .Where(c => c.PropertyId == propertyId && c.EntityType == "P" && c.PropertyDetailsId == null
                        && typeIds.Contains(c.CertificateTypeId) && c.IsActive && !c.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var latestByType = certs
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.CreatedDate ?? DateTime.MinValue).ThenByDescending(c => c.Id).First());

        // Resolve Wing and Society for inheritance fallback
        var wingDetailId = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.Id == propertyId && !p.MarkedForDeletion)
            .Select(p => p.WingDetailId)
            .FirstOrDefaultAsync(cancellationToken);

        int? societyDetailId = null;
        if (wingDetailId.HasValue && wingDetailId.Value > 0)
        {
            societyDetailId = await _context.WingDetailsMast.AsNoTracking()
                .Where(w => w.Id == wingDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => (int?)w.SocietyDetailsMastId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!societyDetailId.HasValue)
        {
            societyDetailId = await _context.SocietyDetailsMast.AsNoTracking()
                .Where(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var wingCerts = wingDetailId.HasValue && wingDetailId.Value > 0
            ? await _context.PropertyCertificates.AsNoTracking()
                .Where(c => c.EntityType == "W" && c.WingDetailId == wingDetailId.Value
                            && typeIds.Contains(c.CertificateTypeId) && c.IsActive && !c.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyCertificateEntity>();

        var latestByWing = wingCerts
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.CreatedDate ?? DateTime.MinValue).ThenByDescending(c => c.Id).First());

        var societyCerts = societyDetailId.HasValue && societyDetailId.Value > 0
            ? await _context.PropertyCertificates.AsNoTracking()
                .Where(c => c.EntityType == "S" && c.SocietyDetailId == societyDetailId.Value
                            && typeIds.Contains(c.CertificateTypeId) && c.IsActive && !c.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyCertificateEntity>();

        var latestBySociety = societyCerts
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.CreatedDate ?? DateTime.MinValue).ThenByDescending(c => c.Id).First());

        return types.Select(type =>
        {
            var cert = latestByType.GetValueOrDefault(type.Id)
                       ?? latestByWing.GetValueOrDefault(type.Id)
                       ?? latestBySociety.GetValueOrDefault(type.Id);

            return new CertificateTypeStatusDto
            {
                CertificateTypeId = type.Id,
                CertificateTypeCode = type.CertificateTypeCode,
                CertificateTypeName = type.CertificateTypeName,
                Description = type.Description,
                IsIssued = cert != null,
                CertificateNo = cert?.CertificateNo,
                IssueDate = cert?.IssueDate,
                CreatedBy = cert?.CreatedBy,
                CreatedDate = cert?.CreatedDate,
                UpdatedBy = cert?.UpdatedBy,
                UpdatedDate = cert?.UpdatedDate,
            };
        }).ToList();
    }
}
