namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Response DTO for common workflow stage property details sub-grid.
/// Shows all properties for a specific zone and workflow stage with new vs old property details comparison.
/// </summary>
public class SubGridPDDataDto
{
    /// <summary>
    /// Workflow stage ID.
    /// </summary>
    public int WorkflowStageId { get; set; }

    /// <summary>
    /// Workflow stage name.
    /// </summary>
    public string WorkflowStageName { get; set; } = string.Empty;

    /// <summary>
    /// Zone ID
    /// </summary>
    public int ZoneId { get; set; }

    /// <summary>
    /// Zone name/division name
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Ward ID when the sub-grid is scoped ward-wise.
    /// </summary>
    public int? WardId { get; set; }

    /// <summary>
    /// Ward number/name when the sub-grid is scoped ward-wise.
    /// </summary>
    public string? WardNo { get; set; }

    /// <summary>
    /// List of property details
    /// </summary>
    public List<SubGridPropertyDetailsDto> Properties { get; set; } = new();

    /// <summary>
    /// Total count of properties
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Property details for common workflow stage sub-grid including new vs old comparison.
/// </summary>
public class SubGridPropertyDetailsDto
{
    /// <summary>
    /// Property ID
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// Formatted Property Number (WardNo-PropertyNo-PartitionNo)
    /// </summary>
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Property category name (निवासी/Commercial/etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Property type description
    /// </summary>
    public string PropertyDescription { get; set; } = string.Empty;

    /// <summary>
    /// Property type code/name used by approval grids.
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;

    /// <summary>
    /// Owner name
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Occupier name
    /// </summary>
    public string OccupierName { get; set; } = string.Empty;

    /// <summary>
    /// Mobile number
    /// </summary>
    public string MobileNo { get; set; } = string.Empty;

    /// <summary>
    /// Property address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Flat or shop name
    /// </summary>
    public string FlatOrShopName { get; set; } = string.Empty;

    /// <summary>
    /// Wing name linked with the property society details, when available.
    /// </summary>
    public string WingName { get; set; } = string.Empty;

    /// <summary>
    /// Assessment status (Assessed/Unassessed/etc.)
    /// </summary>
    public string AssessmentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Count of property details floors
    /// </summary>
    public int FloorCount { get; set; }

    /// <summary>
    /// Document GUID for photos/documents
    /// </summary>
    public string? DocumentGuid { get; set; }

    /// <summary>
    /// Building plan document GUID, when a plan document is linked.
    /// </summary>
    public string? PlanDocumentGuid { get; set; }

    /// <summary>
    /// Extra revenue generated from new demand compared with old demand.
    /// </summary>
    public decimal AdditionalRevenue { get; set; }

    /// <summary>
    /// New vs Old property details comparison
    /// </summary>
    public PropertyDetailsComparisonDto PropertyDetailsComparison { get; set; } = new();
}

/// <summary>
/// Response DTO for pending Assessment properties that need QC checklist flags.
/// </summary>
public class PendingAssessmentSubGridPDDataDto
{
    public int WorkflowStageId { get; set; }

    public string WorkflowStageName { get; set; } = string.Empty;

    public int ZoneId { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public int? WardId { get; set; }

    public string? WardNo { get; set; }

    public List<PendingAssessmentSubGridPropertyDetailsDto> Properties { get; set; } = new();

    public int TotalCount { get; set; }
}

/// <summary>
/// Pending Assessment property details including QC checklist state.
/// </summary>
public class PendingAssessmentSubGridPropertyDetailsDto : SubGridPropertyDetailsDto
{
    public AssessmentQcChecklistDto QcChecklist { get; set; } = new();
}

/// <summary>
/// Checklist flags shown before sending Assessment properties for approval.
/// </summary>
public class AssessmentQcChecklistDto
{
    public bool SiteQc { get; set; }

    public bool ApplyTaxes { get; set; }

    public bool OfficeQc { get; set; }

    public bool DataUpdated { get; set; }

    public bool AddTaxes { get; set; }

    public bool OcCcBill { get; set; }
}

/// <summary>
/// Comparison of new vs old property details
/// </summary>
public class PropertyDetailsComparisonDto
{
    /// <summary>
    /// New property details
    /// </summary>
    public PropertyDetailsValueDto NewRecord { get; set; } = new();

    /// <summary>
    /// Old property details
    /// </summary>
    public PropertyDetailsValueDto OldRecord { get; set; } = new();
}

/// <summary>
/// Property details values (area, use, tax, etc.)
/// </summary>
public class PropertyDetailsValueDto
{
    /// <summary>
    /// Area (निर्मित क्षेत्र)
    /// </summary>
    public string Area { get; set; } = "N/A";

    /// <summary>
    /// Use type (Residential, Commercial, etc.)
    /// </summary>
    public string Use { get; set; } = "N/A";

    /// <summary>
    /// Rental Value (किराया मूल्य)
    /// </summary>
    public string RV { get; set; } = "N/A";

    /// <summary>
    /// Capital Value / Tax (पूंजी मूल्य)
    /// </summary>
    public string CTax { get; set; } = "N/A";

    /// <summary>
    /// Rental Tax
    /// </summary>
    public string RTax { get; set; } = "N/A";

    /// <summary>
    /// Total Tax (एकूण कर)
    /// </summary>
    public string TotalTax { get; set; } = "N/A";
}

[Obsolete("Use SubGridPDDataDto for common workflow stage sub-grid data.")]
public class GeoSequencingPDGridDto : SubGridPDDataDto
{
}

[Obsolete("Use SubGridPropertyDetailsDto for common workflow stage sub-grid property details.")]
public class GeoSequencingPropertyDetailsDto : SubGridPropertyDetailsDto
{
}
