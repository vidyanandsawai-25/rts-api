using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSApplicationApproval;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSApplicationApprovalService:ICommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>
{
    //<summary>
    //Dashboard card data for RTS Application Approval get all applications with pagination and filtering
    Task<RTSApplicationDashboardCardsCountDto> GetDashboardCardsDataAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RTSApplicationDashboardDetailsDto>> GetAllDashboardApplicationAsync(RTSApplicationQueryParameters queryParameters, CancellationToken cancellationToken = default);

    //<summary>
    //Get application approval stages and application Deatilstatus id for drawer of view details
    Task<RTSApplicationViewDetailsDto?> ViewApplicationApprovalSummaryAsync(int applicationId, CancellationToken cancellationToken = default);
    Task<ApplicationApprovalStageDetailsDto?> GetApplicationApprovalStagesAsync(int applicationId, CancellationToken cancellationToken = default);

    // <summary>
    // verify application document by application id and document id or application details id and document id
    Task<CurrentApprovalOfficerDto?> GetCurrentApprovalOfficerAsync(int applicationId, CancellationToken cancellationToken = default);

    // <summary>
    // Clerk opertation verify Document Revert Application and Make Minor Correction In Application 
    Task<RTSApplicationApprovalResponseDto> VerifyDocumentsAndProcessApplicationAsync(int applicationId, UpdateRTSApplicationProcessDto dto, CancellationToken cancellationToken = default);
    Task<RTSApplicationApprovalResponseDto> VerifyAndCorrectApplicationAsync(int applicationId,UpdateRTSApplicationVerificationDto dto, CancellationToken cancellationToken = default);
    Task<RTSApplicationApprovalResponseDto> VerifyAndRevertApplicationAsync(int applicationId, UpdateRTSApplicationProcessDto dto,CancellationToken cancellationToken = default);


    // <summary>
    //Application approval officers operation verify application and sent to next approval officer or reject application Until last approval
    Task<RTSApplicationApprovalResponseDto> VerifyApplicationAndSentToApproveAsync(int applicationId, UpdateRTSApplicationProcessDto dto, CancellationToken cancellationToken = default);
    Task<RTSApplicationApprovalResponseDto> RejectApplicationByOfficerAsync (int applicationId, UpdateRTSApplicationProcessDto dto,CancellationToken cancellationToken = default);


}
