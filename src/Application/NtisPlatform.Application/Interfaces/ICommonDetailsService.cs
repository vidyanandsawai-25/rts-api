using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ICommonDetailsService
{
    Task<List<BulkUpdateMasterDto>> GetMenuAsync(CancellationToken ct);
    Task<List<BulkUpdateFieldConfigDto>> GetFormFieldsAsync(string updateCode, CancellationToken ct);
    Task<List<PreviewGridColumnDto>> GetGridColumnsAsync(string updateCode, CancellationToken ct);
    Task<PagedResult<PropertyPreviewDto>> FilterPropertiesAsync(FilterPropertiesRequestDto request, CancellationToken ct);
    Task<PagedResult<PropertyPreviewDto>> FilterPropertiesByCategoryAsync(FilterPropertiesByCategoryRequestDto request, CancellationToken ct);
    Task<BulkUpdateResultDto> BulkUpdateAsync(BulkUpdateRequestDto request, int updatedBy, string? ipAddress, CancellationToken ct);
    Task<byte[]> ExportPropertiesToExcelAsync(FilterPropertiesRequestDto request, CancellationToken ct);
    Task<BulkUpdateResultDto> ImportPropertiesFromExcelAsync(
        string updateCode, Stream fileStream, int updatedBy, string? ipAddress, CancellationToken ct);

    Task<PagedResult<UpdateHistoryDto>> GetUpdateHistoryAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct);

    Task<byte[]> ExportUpdateHistoryToExcelAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct);
}
