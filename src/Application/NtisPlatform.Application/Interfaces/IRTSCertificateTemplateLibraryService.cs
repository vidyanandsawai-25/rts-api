using NtisPlatform.Application.DTOs.RTSCertificate;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSCertificateTemplateLibraryService
{
    Task<List<RTSCertificateLibraryTemplateDto>> GetAllAsync(CancellationToken ct);
    Task<RTSCertificateLibraryTemplateDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<RTSCertificateLibraryTemplateDto> CreateAsync(CreateRTSCertificateLibraryTemplateDto dto, int userId, CancellationToken ct);
    Task<RTSCertificateLibraryTemplateDto> UpdateAsync(UpdateRTSCertificateLibraryTemplateDto dto, int userId, CancellationToken ct);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct);
}
