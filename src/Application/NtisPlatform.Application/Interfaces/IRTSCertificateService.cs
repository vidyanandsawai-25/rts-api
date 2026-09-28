using NtisPlatform.Application.DTOs.RTSCertificate;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSCertificateService
{
    // Templates CRUD & Available Tags
    Task<List<RTSCertificateTemplateDto>> GetAllTemplatesAsync(CancellationToken ct);
    Task<RTSCertificateTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken ct);
    Task<RTSCertificateTemplateDto?> GetTemplateByServiceIdAsync(int serviceId, CancellationToken ct);
    Task<List<CertificateAvailableTagDto>> GetAvailableTagsForServiceAsync(int serviceId, CancellationToken ct);
    Task<RTSCertificateTemplateDto> CreateTemplateAsync(CreateRTSCertificateTemplateDto dto, int userId, CancellationToken ct);
    Task<RTSCertificateTemplateDto> UpdateTemplateAsync(UpdateRTSCertificateTemplateDto dto, int userId, CancellationToken ct);
    Task<bool> DeleteTemplateAsync(int id, int userId, CancellationToken ct);

    // Live Preview for Approving Officer
    Task<CertificatePreviewResponseDto> PreviewCertificateAsync(CertificatePreviewRequestDto request, CancellationToken ct);

    // Issue Certificate on Final Stage Approval
    Task<RTSIssuedCertificateDto> IssueCertificateAsync(IssueCertificateRequestDto request, int userId, CancellationToken ct);

    // Fetch Issued Certificate for Citizen / Admin
    Task<RTSIssuedCertificateDto?> GetIssuedCertificateByApplicationNoAsync(string applicationNo, CancellationToken ct);
    Task<RTSIssuedCertificateDto?> GetIssuedCertificateByGuidAsync(Guid certificateGuid, CancellationToken ct);

    // Public QR Verification
    Task<CertificateVerificationResponseDto> VerifyCertificatePublicAsync(string identifier, CancellationToken ct);
}
