using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;

public class RetrospectiveRuleMasterDto : BaseDtos
{
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? RuleDescription { get; set; }
    public int PriorityNo { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public bool IsFallbackRule { get; set; }
    public string RuleStatus { get; set; } = string.Empty;
    public string? AuthorizationStatus { get; set; }
    public bool LegalCapEnabled { get; set; }
    public int LegalCapYears { get; set; }
    public int NoticeDays { get; set; }
    public string? VersionNo { get; set; }
    public string? ResolutionRef { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Remarks { get; set; }
}

public class CreateRetrospectiveRuleMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleMaster_RuleCode_MaxLen_50")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleName_Required")]
    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_RuleName_MaxLen_200")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_RuleDescription_MaxLen_1000")]
    public string? RuleDescription { get; set; }

    public int PriorityNo { get; set; }

    /// <summary>CONDITION_BASED / EXACT_EVIDENCE_MATCH / PRIORITY_BASED</summary>
    [Required(ErrorMessage = "RetrospectiveRuleMaster_MatchType_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_MatchType_MaxLen_30")]
    public string MatchType { get; set; } = "CONDITION_BASED";

    public bool IsFallbackRule { get; set; }

    /// <summary>Draft / Active / Review / NeedsClarification</summary>
    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleStatus_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_RuleStatus_MaxLen_30")]
    public string RuleStatus { get; set; } = "Draft";

    /// <summary>AUTHORIZED / UNAUTHORIZED / UNDETERMINED</summary>
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_AuthorizationStatus_MaxLen_30")]
    public string? AuthorizationStatus { get; set; }

    public bool LegalCapEnabled { get; set; } = true;
    public int LegalCapYears { get; set; } = 6;
    public int NoticeDays { get; set; } = 15;

    [StringLength(20, ErrorMessage = "RetrospectiveRuleMaster_VersionNo_MaxLen_20")]
    public string? VersionNo { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_ResolutionRef_MaxLen_200")]
    public string? ResolutionRef { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }
}

public class UpdateRetrospectiveRuleMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleMaster_RuleCode_MaxLen_50")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleName_Required")]
    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_RuleName_MaxLen_200")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_RuleDescription_MaxLen_1000")]
    public string? RuleDescription { get; set; }

    public int PriorityNo { get; set; }

    [Required(ErrorMessage = "RetrospectiveRuleMaster_MatchType_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_MatchType_MaxLen_30")]
    public string MatchType { get; set; } = "CONDITION_BASED";

    public bool IsFallbackRule { get; set; }

    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleStatus_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_RuleStatus_MaxLen_30")]
    public string RuleStatus { get; set; } = "Draft";

    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_AuthorizationStatus_MaxLen_30")]
    public string? AuthorizationStatus { get; set; }

    public bool LegalCapEnabled { get; set; }
    public int LegalCapYears { get; set; }
    public int NoticeDays { get; set; }

    [StringLength(20, ErrorMessage = "RetrospectiveRuleMaster_VersionNo_MaxLen_20")]
    public string? VersionNo { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_ResolutionRef_MaxLen_200")]
    public string? ResolutionRef { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }
}
