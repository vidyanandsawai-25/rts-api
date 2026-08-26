using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisDepartmentFilterMappingDto : BaseDtos
{
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int FilterMasterId { get; set; }
    public string? FilterKey { get; set; }
    public string? DefaultFilterLabel { get; set; }
    public string? CustomLabel { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateGisDepartmentFilterMappingDto : CreateBaseDtos
{
    public int DepartmentId { get; set; }
    public int FilterMasterId { get; set; }
    public string? CustomLabel { get; set; }
    public int DisplayOrder { get; set; } = 1;
}

public class UpdateGisDepartmentFilterMappingDto : UpdateBaseDtos
{
    public string? CustomLabel { get; set; }
    public int DisplayOrder { get; set; }
}

public class GisDepartmentFilterMappingQueryParameters : BaseQueryParameters
{
    public int? DepartmentId { get; set; }
    public int? FilterMasterId { get; set; }
}
