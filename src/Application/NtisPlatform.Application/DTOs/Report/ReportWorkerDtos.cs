namespace NtisPlatform.Application.DTOs.Report;

// ─── Worker handshake / data-pull / upload contract ──────────────────────────
// Hand-mirrored on the ntis-report worker side (.NET Framework 4.8 cannot share this assembly).

/// <summary>Body the worker POSTs to /api/Report/worker/authenticate.</summary>
public class WorkerAuthenticateRequestDto
{
    public string ShortLivedToken { get; set; } = string.Empty;
    public Guid ReportRequestId { get; set; }
}

/// <summary>Result of a successful worker authentication (SLT → LLT).</summary>
public class WorkerAuthenticateResultDto
{
    public string LongLivedToken { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;        // ReportDefinition.TemplateFile
    public string DataProviderCode { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public List<ReportSectionDescriptor> Sections { get; set; } = new();
    public string OutputFormat { get; set; } = "pdf";
}

/// <summary>Body the worker POSTs to /api/Report/worker/data (Bearer LLT).</summary>
public class WorkerDataRequestDto
{
    public Guid ReportRequestId { get; set; }
    public string Section { get; set; } = "main";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
}

/// <summary>Result of /api/Report/worker/upload.</summary>
public class WorkerUploadResultDto
{
    public Guid DocumentGuid { get; set; }
}
