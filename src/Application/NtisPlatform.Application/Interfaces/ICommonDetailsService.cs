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
    Task<List<BulkUpdateResultDto>> BulkUpdateBatchAsync(List<BulkUpdateRequestDto> requests, int updatedBy, string? ipAddress, CancellationToken ct);
    Task<byte[]> ExportPropertiesToExcelAsync(ExportPropertiesRequestDto request, CancellationToken ct);
    Task<BulkUpdateResultDto> ImportPropertiesFromExcelAsync(
        string updateCode, Stream fileStream, int updatedBy, string? ipAddress, string? remarks, CancellationToken ct);
    Task<ExcelValidationResultDto> ValidateImportExcelAsync(string updateCode, Stream fileStream, CancellationToken ct);

    Task<PagedResult<UpdateHistoryDto>> GetUpdateHistoryAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct);

    Task<byte[]> ExportUpdateHistoryToExcelAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct);

    Task<PagedResult<UpdateActivityDto>> GetUpdateActivityAsync(
        UpdateActivityQueryParameters request, CancellationToken ct);

    Task<byte[]> ExportUpdateActivityToExcelAsync(
        UpdateActivityQueryParameters request, CancellationToken ct);

    Task<List<SourceTableLookupDto>> GetSourceTablesAsync(CancellationToken ct);
    Task<List<SourceTableFieldLookupDto>> GetSourceTableFieldsAsync(int sourceTableId, CancellationToken ct);

    Task<BulkUpdateDefinitionResultDto> CreateFromSourceTableAsync(
        CreateBulkUpdateDefinitionFromSourceDto request, int createdBy, CancellationToken ct);
}
