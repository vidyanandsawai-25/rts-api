using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Default Excel upload service implementation shared across modules.
/// </summary>
public class ExcelUploadService : IExcelUploadService
{
    public (List<string> Headers, List<ExcelRow> Rows) Read(Stream stream)
        => ExcelImportHelper.Read(stream);

    public List<string> GetMissingRequiredHeaders(
        IEnumerable<string> headers,
        IEnumerable<string> requiredHeaders)
    {
        var headerSet = new HashSet<string>(
            headers.Where(h => !string.IsNullOrWhiteSpace(h)),
            StringComparer.OrdinalIgnoreCase);

        return requiredHeaders
            .Where(h => !headerSet.Contains(h))
            .ToList();
    }
}
