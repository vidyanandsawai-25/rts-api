using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Orchestrates the Property Tax Operations screen: initial load, scope/eligibility evaluation,
/// synchronous "Add Tax" execution (reusing the rateable-value engine), and audit/monitor reads.
/// </summary>
public interface IPropertyTaxOperationsService
{
    Task<OperationsInitDto> GetInitAsync(int actingUserId, CancellationToken cancellationToken = default);

    Task<EligibleCountResponseDto> GetEligibleCountAsync(
        EligibleCountRequestDto request, int actingUserId, CancellationToken cancellationToken = default);

    Task<OperationPreviewResponseDto> GetPreviewAsync(
        OperationPreviewRequestDto request, int actingUserId, CancellationToken cancellationToken = default);

    Task<ExecuteOperationResponseDto> ExecuteAsync(
        ExecuteOperationRequestDto request, OperationContext context, CancellationToken cancellationToken = default);

    Task<JobStatusDto> GetJobStatusAsync(
        int jobId, int actingUserId, CancellationToken cancellationToken = default);

    Task<PagedResult<JobPropertyResultDto>> GetJobPropertiesAsync(
        int jobId, JobPropertiesQueryParameters query, int actingUserId, CancellationToken cancellationToken = default);

    Task<PagedResult<JobAuditDto>> GetAuditListAsync(
        OperationAuditQueryParameters query, int actingUserId, CancellationToken cancellationToken = default);

    Task<JobAuditDetailDto> GetAuditDetailAsync(
        int jobId, JobPropertiesQueryParameters queryParams, int actingUserId, CancellationToken cancellationToken = default);

    Task<ImportTemplateDto> GetImportTemplateAsync(CancellationToken cancellationToken = default);

    Task ProcessJobAsync(int jobId, CancellationToken cancellationToken = default);
}
