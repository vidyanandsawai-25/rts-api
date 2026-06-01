namespace NtisPlatform.Application.Options;

public sealed class ApartmentQCOptions
{
    public const string Section = "ApartmentQC";

    /// <summary>
    /// Per-request body size cap (bytes) applied to the bulk-update endpoint via
    /// <see cref="Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute"/>.
    /// Must be a compile-time constant because it is used as an attribute argument.
    /// Change here; the controller attribute picks it up automatically.
    /// </summary>
    public const long BulkUpdateRequestSizeLimit = 1_048_576; // 1 MB

    /// <summary>
    /// Maximum number of records returned when PageSize = -1 (unpaged / export).
    /// Defaults to 1000.
    /// </summary>
    public int MaxUnpagedPageSize { get; set; } = 1000;

    /// <summary>
    /// Maximum number of PropertyDetails rows allowed in a single bulk-update request.
    /// Defaults to 500.
    /// </summary>
    public int MaxBulkUpdateBatchSize { get; set; } = 500;

    /// <summary>
    /// Maximum number of rows the Apartment QC Excel export will produce in a single call.
    /// When the active filter set yields more rows, the export endpoint returns HTTP 400
    /// telling the caller to narrow the filter. Defaults to 50,000.
    /// </summary>
    public int MaxExportRowCount { get; set; } = 50_000;
}
