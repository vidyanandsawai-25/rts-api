using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Request to send one or more Assessment properties to Clerk approval.
/// </summary>
public class SendToApproveRequestDto
{
    /// <summary>
    /// Property ids to send for approval. Send one id for a single property or multiple ids for bulk approval.
    /// </summary>
    [Required(ErrorMessage = "The PropertyIds are Required")]
    [PositiveIntegerCollection(ErrorMessage = "The PropertyIds are Required")]
    public List<int> PropertyIds { get; set; } = new();

    [Range(1, int.MaxValue, ErrorMessage = "The UserId is Required")]
    public int UserId { get; set; }
}

/// <summary>
/// Validates that an integer collection contains at least one positive id and no invalid ids.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public  class PositiveIntegerCollectionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is not IEnumerable<int> values)
            return ValidationResult.Success;

        var ids = values.ToList();
        return ids.Count > 0 && ids.All(id => id > 0)
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} must contain positive ids only.",
                new[] { validationContext.MemberName ?? validationContext.DisplayName });
    }
}

/// <summary>
/// Response after sending Assessment properties to approval.
/// </summary>
public class SendToApproveResponseDto
{
    public bool IsInserted { get; set; }
    public int PropertyId { get; set; }
    public List<int> PropertyIds { get; set; } = new();
    public int UserId { get; set; }
    public int SignAuthorityId { get; set; }
    public string AuthorityCode { get; set; } = string.Empty;
    public int RequestedCount { get; set; }
    public int InsertedCount { get; set; }
    public List<int> InsertedPropertyIds { get; set; } = new();
    public List<int> MissingPropertyIds { get; set; } = new();
    public List<int> AlreadySentPropertyIds { get; set; } = new();
    public List<int> InvalidPropertyIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
