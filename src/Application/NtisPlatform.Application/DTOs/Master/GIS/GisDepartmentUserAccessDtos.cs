using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisDepartmentUserAccessDto : BaseDtos
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int UlbId { get; set; }
    public int ZoneId { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}

public class CreateGisDepartmentUserAccessDto : CreateBaseDtos
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    public int UlbId { get; set; } = 1;
    public int ZoneId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}

public class UpdateGisDepartmentUserAccessDto : UpdateBaseDtos
{
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}

public class GisDepartmentUserAccessQueryParameters : BaseQueryParameters
{
    public int? UserId { get; set; }
    public int? DepartmentId { get; set; }
    public int? ZoneId { get; set; }
}
