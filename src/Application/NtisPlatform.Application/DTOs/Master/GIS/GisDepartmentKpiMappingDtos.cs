using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisDepartmentKpiMappingDto : BaseDtos
{
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int KpiMasterId { get; set; }
    public string? KpiCode { get; set; }
    public string? DefaultTitle { get; set; }
    public string? CustomTitle { get; set; }
    public string? CustomIcon { get; set; }
    public string? CustomColor { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateGisDepartmentKpiMappingDto : CreateBaseDtos
{
    public int DepartmentId { get; set; }
    public int KpiMasterId { get; set; }
    public string? CustomTitle { get; set; }
    public string? CustomIcon { get; set; }
    public string? CustomColor { get; set; }
    public int DisplayOrder { get; set; } = 1;
}

public class UpdateGisDepartmentKpiMappingDto : UpdateBaseDtos
{
    public string? CustomTitle { get; set; }
    public string? CustomIcon { get; set; }
    public string? CustomColor { get; set; }
    public int DisplayOrder { get; set; }
}

public class GisDepartmentKpiMappingQueryParameters : BaseQueryParameters
{
    public int? DepartmentId { get; set; }
    public int? KpiMasterId { get; set; }
}
