namespace NtisPlatform.Application.DTOs.Report;

/// <summary>
/// Describes one logical data section of a report (a Crystal main report or a subreport).
/// </summary>
/// <param name="Section">Section key. "main" for the main report; otherwise the subreport name.</param>
/// <param name="Paginated">
/// True if the section is fetched page-by-page; false if returned whole on page 1
/// (used for small sections such as the owners list / group header).
/// </param>
public sealed record ReportSectionDescriptor(string Section, bool Paginated);

/// <summary>
/// One page of report data for a section. <see cref="Rows"/> are arbitrary row objects that the
/// worker accumulates and serializes into the JSON the Crystal template binds to.
/// </summary>
public sealed class ReportDataPage
{
    public string Section { get; set; } = "main";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public IReadOnlyList<object> Rows { get; set; } = Array.Empty<object>();
}
