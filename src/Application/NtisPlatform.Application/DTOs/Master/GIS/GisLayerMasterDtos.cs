using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisLayerMasterDto : BaseDtos
{
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int UlbId { get; set; }
    public string LayerCode { get; set; } = null!;
    public string LayerName { get; set; } = null!;
    public string GeometryType { get; set; } = null!;
    public string? StyleConfigJson { get; set; }
    public string? PopupSchemaJson { get; set; }
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public bool IsDefaultVisible { get; set; }
}

public class CreateGisLayerMasterDto : CreateBaseDtos
{
    public int DepartmentId { get; set; }
    public int UlbId { get; set; } = 1;
    public string LayerCode { get; set; } = null!;
    public string LayerName { get; set; } = null!;
    public string GeometryType { get; set; } = null!;
    public string? StyleConfigJson { get; set; }
    public string? PopupSchemaJson { get; set; }
    public int MinZoom { get; set; } = 10;
    public int MaxZoom { get; set; } = 20;
    public bool IsDefaultVisible { get; set; } = true;
}

public class UpdateGisLayerMasterDto : UpdateBaseDtos
{
    public string LayerName { get; set; } = null!;
    public string? StyleConfigJson { get; set; }
    public string? PopupSchemaJson { get; set; }
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public bool IsDefaultVisible { get; set; }
}

public class GisLayerMasterQueryParameters : BaseQueryParameters
{
    public int? DepartmentId { get; set; }
    public string? LayerCode { get; set; }
    public string? GeometryType { get; set; }
}
