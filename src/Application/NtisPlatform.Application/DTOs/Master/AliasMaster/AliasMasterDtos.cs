 using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Read model for a <c>CORE.AliasMaster</c> row — the software field plus its current, live
/// display aliases. The Alias Master screen renders directly from this (no separate approval
/// state — every write is immediate and live).
/// </summary>
public class AliasMasterDto
{
    public int Id { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string LabelName { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? RegionalName { get; set; }
    public string? HindiName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateAliasMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AliasMaster_KeyName_Required")]
    [StringLength(200, ErrorMessage = "AliasMaster_KeyName_MaxLen_200")]
    public string KeyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AliasMaster_LabelName_Required")]
    [StringLength(200, ErrorMessage = "AliasMaster_LabelName_MaxLen_200")]
    public string LabelName { get; set; } = string.Empty;

    // Language names are intentionally optional here — a newly-catalogued field is created with
    // blank aliases (same shape as the seeded rows) and filled in afterward via UpdateAsync.
    [StringLength(200, ErrorMessage = "AliasMaster_EnglishName_MaxLen_200")]
    public string? EnglishName { get; set; }

    [StringLength(200, ErrorMessage = "AliasMaster_RegionalName_MaxLen_200")]
    public string? RegionalName { get; set; }

    [StringLength(200, ErrorMessage = "AliasMaster_HindiName_MaxLen_200")]
    public string? HindiName { get; set; }
}

/// <summary>
/// Only the labels and the active flag can be updated. Submitted directly from the pre-filled Edit modal.
/// </summary>
public class UpdateAliasMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AliasMaster_LabelName_Required")]
    [StringLength(200, ErrorMessage = "AliasMaster_LabelName_MaxLen_200")]
    public string LabelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AliasMaster_EnglishName_Required")]
    [StringLength(200, ErrorMessage = "AliasMaster_EnglishName_MaxLen_200")]
    public string EnglishName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "AliasMaster_RegionalName_MaxLen_200")]
    public string? RegionalName { get; set; }

    [StringLength(200, ErrorMessage = "AliasMaster_HindiName_MaxLen_200")]
    public string? HindiName { get; set; }
}

/// <summary>
/// Pagination/search/sort for <c>GET /api/alias-master</c>. <c>SearchTerm</c> matches across
/// KeyName/LabelName/EnglishName/RegionalName/HindiName; individual fields can also be
/// filtered directly, and KeyName/LabelName/EnglishName are sortable via SortBy.
/// </summary>
public class AliasMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? KeyName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? LabelName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? EnglishName { get; set; }

    [Searchable]
    public string? RegionalName { get; set; }

    [Searchable]
    public string? HindiName { get; set; }
}

/// <summary>
/// Lightweight projection for <c>GET /api/alias-master/active</c> — every active row's
/// per-language names, keyed by <c>KeyName</c>. Consumed by the frontend as a whole-table,
/// unpaged override map for its own JSON-based translations; no id/audit fields needed.
/// </summary>
public class AliasLabelDto
{
    public string KeyName { get; set; } = string.Empty;
    public string? EnglishName { get; set; }
    public string? RegionalName { get; set; }
    public string? HindiName { get; set; }
}

/// <summary>
/// Summary counts of field records (Total, Active, Inactive) for <c>CORE.AliasMaster</c>.
/// </summary>
public class AliasMasterCountDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

