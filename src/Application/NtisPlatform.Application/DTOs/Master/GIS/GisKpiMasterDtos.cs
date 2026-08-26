using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisKpiMasterDto : BaseDtos
{
    public string KpiCode { get; set; } = null!;
    public string DefaultTitle { get; set; } = null!;
    public string DefaultIcon { get; set; } = null!;
    public string DefaultColor { get; set; } = null!;
}

public class CreateGisKpiMasterDto : CreateBaseDtos
{
    public string KpiCode { get; set; } = null!;
    public string DefaultTitle { get; set; } = null!;
    public string DefaultIcon { get; set; } = "fa-building";
    public string DefaultColor { get; set; } = "#0078FF";
}

public class UpdateGisKpiMasterDto : UpdateBaseDtos
{
    public string DefaultTitle { get; set; } = null!;
    public string DefaultIcon { get; set; } = null!;
    public string DefaultColor { get; set; } = null!;
}

public class GisKpiMasterQueryParameters : BaseQueryParameters
{
    public string? KpiCode { get; set; }
}
