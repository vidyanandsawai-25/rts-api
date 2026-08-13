using NtisPlatform.Application.Helpers;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Reusable Excel upload reader/validator that can be used by any module.
/// </summary>
public interface IExcelUploadService
{
    (List<string> Headers, List<ExcelRow> Rows) Read(Stream stream);

    List<string> GetMissingRequiredHeaders(
        IEnumerable<string> headers,
        IEnumerable<string> requiredHeaders);
}
