using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSRuleMaster;

/// <summary>Inputs supplied by a citizen form for an RTS fee calculation.</summary>
public sealed class RTSFeeEvaluationRequestDto
{
    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }

    public List<RTSFeeFieldValueDto> FieldValues { get; set; } = [];
}

/// <summary>
/// A normalized dynamic-form value. FieldCode is optional legacy support; the
/// FieldDefinitionId is the canonical rule-to-form link.
/// </summary>
public sealed class RTSFeeFieldValueDto
{
    [Range(1, int.MaxValue)]
    public int FieldDefinitionId { get; set; }

    public string? FieldCode { get; set; }
    public string? Value { get; set; }
}

public sealed class RTSFeeMatchedRuleDto
{
    public int RuleId { get; set; }
    public string RuleActionType { get; set; } = string.Empty;
    public string? CalculationType { get; set; }
    public int? ConditionFieldDefinitionId { get; set; }
    public string? ConditionField { get; set; }
    public string? ExpectedValue { get; set; }
    public decimal Amount { get; set; }
    public int Priority { get; set; }
}

public sealed class RTSFeeEvaluationResponseDto
{
    public int ServiceId { get; set; }
    public decimal StaticServiceFee { get; set; }
    public decimal BaseFee { get; set; }
    public decimal AddonFee { get; set; }
    public decimal PayableAmount { get; set; }
    public bool UsedServiceFeeFallback { get; set; }
    public List<RTSFeeMatchedRuleDto> MatchedRules { get; set; } = [];
}
