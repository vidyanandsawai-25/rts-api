using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>
/// Describes which properties an operation targets. The frontend sends resolved IDs (zone/ward/
/// property-type) rather than display strings. Which fields are required depends on the
/// owning request's ScopeType — that cross-field rule is enforced in the service, not here.
/// </summary>
public class OperationScopeDto
{
    /// <summary>Multi-zone selection for Zone-scope operations.</summary>
    public List<int>? ZoneIds { get; set; }
    public List<int>? WardIds { get; set; }

    public List<int>? PropertyTypeIds { get; set; }

    public List<int>? AssessmentStatusIds { get; set; }

    public List<string>? Building { get; set; }
    public string? FromPropertyNo { get; set; }
    public string? ToPropertyNo { get; set; }

    /// <summary>
    /// Optional partition numbers to narrow selection within Building or Range scopes.
    /// Supports single or multiple values (alphanumeric). Null means all partitions are included.
    /// </summary>
    public List<string>? PartitionNos { get; set; }

    public List<int>? PropertyIds { get; set; }

    /// <summary>Free-text search for Property-Wise scope (mobile / UPIC).</summary>
    public string? SearchText { get; set; }
    public List<string>? UpicIds { get; set; }
    public List<string>? MobileNumbers { get; set; }
}
