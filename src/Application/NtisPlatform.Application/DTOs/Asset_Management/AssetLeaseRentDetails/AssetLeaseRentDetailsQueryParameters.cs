using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;

public class AssetLeaseRentDetailsQueryParameters : BaseQueryParameters
{
    // AssetCategoryId/AssetTypeId/ParentAssetId/ZoneId/WardId/AssetNo (below) were previously
    // declared [Filterable(EntityProperty = "Asset.X")] / [Searchable], but FilterExpressionBuilder
    // resolves EntityProperty via a single Type.GetProperty(name) call - it has no support for
    // dotted/nested paths, so "Asset.X" would throw FilterValidationException ("Property 'Asset.X'
    // not found on entity type 'AssetLeaseRentDetailsEntity'") the moment this DTO was wired up to
    // a service. Two of these (ZoneId/WardId) were additionally pointing at the wrong entity
    // entirely: they live on AssetDetailsEntity (via AssetMasterEntity.Details), not on
    // AssetMasterEntity itself, so even a hypothetical one-level nested-path feature wouldn't
    // resolve them. Extending FilterExpressionBuilder to support nested paths would change
    // behavior for every other QueryParameters class in the app that uses it - out of proportion
    // for a DTO with no consuming service yet. Following the existing AssetMasterQueryParameters
    // convention (see ZoneId/WardId/Address there) these are left as plain, undecorated properties
    // documenting the join a future service must apply explicitly; AssetId (below) is genuinely a
    // direct property on AssetLeaseRentDetailsEntity and keeps its [Filterable].

    /// <summary>Category filter - one-hop join to AssetMaster.AssetCategoryId, applied in the service.</summary>
    public int? AssetCategoryId { get; set; }

    /// <summary>Zone filter - two-hop join via AssetMaster.Details.ZoneId (AssetDetailsEntity), applied in the service.</summary>
    public int? ZoneId { get; set; }

    /// <summary>Asset type filter - one-hop join to AssetMaster.AssetTypeId, applied in the service.</summary>
    public int? AssetTypeId { get; set; }

    /// <summary>Ward filter - two-hop join via AssetMaster.Details.WardId (AssetDetailsEntity), applied in the service.</summary>
    public int? WardId { get; set; }

    /// <summary>Parent (building) asset filter - one-hop join to AssetMaster.ParentAssetId, applied in the service.</summary>
    public int? ParentAssetId { get; set; }

    [Filterable]
    public int? AssetId { get; set; }

    // RentStatus/PaymentStatus (unlike WorkflowStatus below) have the identical "property not
    // found" problem as the join-dependent properties above, discovered during this review:
    // AssetLeaseRentDetailsEntity has no RentStatus or PaymentStatus column at all - only
    // WorkflowStatus ('Pending'/'Verified'/'Approved'/'Rejected', per the
    // CK_AssetLeaseRentDetails_WorkflowStatus check constraint). [Filterable] with no
    // EntityProperty override resolves by property name, so either of these would throw
    // FilterValidationException the moment used. What these two are actually meant to represent
    // isn't decided by the entity model as it exists today (e.g. a computed "is the lease
    // currently active" derived from LeaseStartDate/LeaseEndDate, or a rollup of the unrelated
    // LeaseRentBillTransaction.PaymentStatus/MonthWiseDemand.DemandStatus tables) - left as plain
    // properties pending that decision rather than guessing a mapping.

    /// <summary>Rent status filter - no corresponding column on AssetLeaseRentDetailsEntity today; needs a decision on what this should resolve to before it can be wired up.</summary>
    public string? RentStatus { get; set; }

    [Filterable]
    public string? WorkflowStatus { get; set; }

    /// <summary>Payment status filter - no corresponding column on AssetLeaseRentDetailsEntity today; needs a decision on what this should resolve to before it can be wired up.</summary>
    public string? PaymentStatus { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? TenantName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ShopName { get; set; }

    /// <summary>Asset number filter/search - one-hop join to AssetMaster.AssetNo, applied in the
    /// service. Previously also carried [Searchable] with no EntityProperty override, which
    /// defaults to looking up "AssetNo" directly on AssetLeaseRentDetailsEntity - since that
    /// property doesn't exist there either, BuildSearchExpression silently skipped it (returns
    /// null property, no error), so the search term was silently never applied. Removed for the
    /// same reason as the [Filterable] attributes above.</summary>
    public string? AssetNo { get; set; }

    [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "LeaseStartDate")]
    public DateTime? FromDate { get; set; }

    [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "LeaseEndDate")]
    public DateTime? ToDate { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }
}
