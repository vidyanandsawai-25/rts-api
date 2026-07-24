using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.AssetRoomType;

public class AssetRoomTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? AssetCategoryId { get; set; }

    [Filterable]
    public int? AssetTypeId { get; set; }

    [Filterable, Searchable, Sortable]
    public string? RoomTypeCode { get; set; }

    [Filterable, Searchable, Sortable]
    public string? RoomTypeName { get; set; }

    [Filterable, Searchable, Sortable]
    public string? Description { get; set; }

    [Filterable(FilterOperator.Equals), Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
