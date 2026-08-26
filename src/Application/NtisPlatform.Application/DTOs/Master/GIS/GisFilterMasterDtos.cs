using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisFilterMasterDto : BaseDtos
{
    public string FilterKey { get; set; } = null!;
    public string FilterLabel { get; set; } = null!;
    public string ControlType { get; set; } = null!;
    public string? ApiSourceUrl { get; set; }
}

public class CreateGisFilterMasterDto : CreateBaseDtos
{
    public string FilterKey { get; set; } = null!;
    public string FilterLabel { get; set; } = null!;
    public string ControlType { get; set; } = "DROPDOWN";
    public string? ApiSourceUrl { get; set; }
}

public class UpdateGisFilterMasterDto : UpdateBaseDtos
{
    public string FilterLabel { get; set; } = null!;
    public string ControlType { get; set; } = null!;
    public string? ApiSourceUrl { get; set; }
}

public class GisFilterMasterQueryParameters : BaseQueryParameters
{
    public string? FilterKey { get; set; }
}
