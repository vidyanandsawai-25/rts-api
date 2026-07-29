namespace NtisPlatform.Core.Models;

/// <summary>
/// Raw database snapshot used by the Automation sub-grid service mapper.
/// </summary>
public sealed class SubGridDataProjection
{
    public int WorkflowStageId { get; set; }
    public string WorkflowStageName { get; set; } = string.Empty;
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int TotalCount { get; set; }

    public List<SubGridPropertyProjection> Properties { get; set; } = new();
    public List<SubGridCountProjection> DetailCounts { get; set; } = new();
    public List<SubGridDocumentProjection> Documents { get; set; } = new();
    public List<SubGridDocumentProjection> PlanDocuments { get; set; } = new();
    public List<SubGridPropertyMapProjection> PropertyMaps { get; set; } = new();
    public List<SubGridNewPropertyDetailProjection> NewDetails { get; set; } = new();
    public List<SubGridOldPropertyDetailProjection> OldDetails { get; set; } = new();
    public List<SubGridTaxValueProjection> NewRvValues { get; set; } = new();
    public List<SubGridTaxValueProjection> NewCurrentTaxes { get; set; } = new();
    public List<SubGridTaxValueProjection> NewPendingTaxes { get; set; } = new();
    public List<SubGridTaxValueProjection> OldCurrentTaxes { get; set; } = new();
    public List<SubGridTaxValueProjection> OldPendingTaxes { get; set; } = new();
    public List<int> ApplyTaxesPropertyIds { get; set; } = new();
    public List<SubGridAssessmentDetailProjection> AssessmentDetails { get; set; } = new();
}

/// <summary>
/// Raw property row for the sub-grid.
/// </summary>
public sealed class SubGridPropertyProjection
{
    public int Id { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TypeDescription { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OccupierName { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string FlatOrShopName { get; set; } = string.Empty;
    public string AssessmentStatusName { get; set; } = string.Empty;
}

public sealed class SubGridCountProjection
{
    public int PropertyId { get; set; }
    public int Count { get; set; }
}

public sealed class SubGridDocumentProjection
{
    public int PropertyId { get; set; }
    public string? DocumentGuid { get; set; }
}

public sealed class SubGridPropertyMapProjection
{
    public int PropertyIdNew { get; set; }
    public int? PropertyIdOld { get; set; }
}

public sealed class SubGridNewPropertyDetailProjection
{
    public int Id { get; set; }
    public decimal Area { get; set; }
    public string Use { get; set; } = string.Empty;
}

public sealed class SubGridOldPropertyDetailProjection
{
    public int Id { get; set; }
    public decimal Area { get; set; }
    public string Use { get; set; } = string.Empty;
    public double OldRV { get; set; }
}

public sealed class SubGridTaxValueProjection
{
    public int PropertyId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class SubGridAssessmentDetailProjection
{
    public int PropertyId { get; set; }
    public DateTime? PartOCDate { get; set; }
    public short? ApplyTaxesFrom { get; set; }
}
