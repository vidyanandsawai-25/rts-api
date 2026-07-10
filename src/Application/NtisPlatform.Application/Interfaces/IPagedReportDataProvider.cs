using NtisPlatform.Application.DTOs.Report;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Optional capability a report data provider implements to support paginated data pulls by the
/// worker. Pagination bounds HTTP/serialization size only — the worker accumulates all pages and
/// the Crystal push model still binds the full dataset before rendering.
///
/// Providers that don't implement this fall back to <see cref="IReportDataProvider.GetDataAsync"/>
/// (single page containing the whole dataset).
/// </summary>
public interface IPagedReportDataProvider : IReportDataProvider
{
    /// <summary>
    /// The data sections this report produces. A flat report has a single "main" section; a report
    /// with subreports lists "main" plus one descriptor per subreport key. Large sections set
    /// <see cref="ReportSectionDescriptor.Paginated"/> = true.
    /// </summary>
    IReadOnlyList<ReportSectionDescriptor> GetSections();

    /// <summary>
    /// Returns one page of rows for the given section. For non-paginated sections, page 1 returns
    /// the whole section with HasMore = false.
    /// </summary>
    Task<ReportDataPage> GetDataPageAsync(
        Dictionary<string, string> parameters,
        string section,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
