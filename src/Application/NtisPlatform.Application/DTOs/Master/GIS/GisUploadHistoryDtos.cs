using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisUploadHistoryDto : BaseDtos
{
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public int RecordCount { get; set; }
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}

public class CreateGisUploadHistoryDto : CreateBaseDtos
{
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = "GeoJSON";
    public int RecordCount { get; set; }
    public string UploadedBy { get; set; } = null!;
}

public class UpdateGisUploadHistoryDto : UpdateBaseDtos
{
    public int RecordCount { get; set; }
}

public class GisUploadHistoryQueryParameters : BaseQueryParameters
{
    public string? FileName { get; set; }
    public string? UploadedBy { get; set; }
}
