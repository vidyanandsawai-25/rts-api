using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface ICertificateTaxGuidelineService
    : ICommonCrudService<CertificateTaxGuidelineEntity, CertificateTaxGuidelineDto, CreateCertificateTaxGuidelineDto, UpdateCertificateTaxGuidelineDto, CertificateTaxGuidelineQueryParameters, int>
{
    Task<object?> GetGuidelineValueAsync(string code, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object?>> GetGuidelineValuesByGroupAsync(string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts a list of certificate tax guideline rows.
    /// Each row is matched by <c>GuidelineCode</c>; existing rows are updated and missing rows are created.
    /// Returns the updated/created DTOs.
    /// </summary>
    Task<IReadOnlyList<CertificateTaxGuidelineDto>> BulkUpsertAsync(
        IReadOnlyList<UpdateCertificateTaxGuidelineDto> items,
        CancellationToken cancellationToken = default);
}
