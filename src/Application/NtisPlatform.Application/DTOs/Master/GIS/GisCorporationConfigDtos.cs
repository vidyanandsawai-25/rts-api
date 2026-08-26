using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class GisCorporationConfigDto : BaseDtos
{
    public int UlbId { get; set; }
    public decimal? DefaultCenterLat { get; set; }
    public decimal? DefaultCenterLng { get; set; }
    public int DefaultZoom { get; set; }
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public string? BoundingBoxJson { get; set; }
    public string? BasemapsJson { get; set; }
    public string PropertyBoundaryColor { get; set; } = "#0078FF";
}

public class CreateGisCorporationConfigDto : CreateBaseDtos
{
    public int UlbId { get; set; }
    public decimal? DefaultCenterLat { get; set; }
    public decimal? DefaultCenterLng { get; set; }
    public int DefaultZoom { get; set; } = 14;
    public int MinZoom { get; set; } = 10;
    public int MaxZoom { get; set; } = 20;
    public string? BoundingBoxJson { get; set; }
    public string? BasemapsJson { get; set; }
    public string PropertyBoundaryColor { get; set; } = "#0078FF";
}

public class UpdateGisCorporationConfigDto : UpdateBaseDtos
{
    public decimal? DefaultCenterLat { get; set; }
    public decimal? DefaultCenterLng { get; set; }
    public int DefaultZoom { get; set; }
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; }
    public string? BoundingBoxJson { get; set; }
    public string? BasemapsJson { get; set; }
    public string PropertyBoundaryColor { get; set; } = "#0078FF";
}

public class GisCorporationConfigQueryParameters : BaseQueryParameters
{
    public int? UlbId { get; set; }
}
