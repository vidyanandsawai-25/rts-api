using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyKyc;

/// <summary>
/// Query parameters used to retrieve common property KYC details
/// using ward, property number, and optional partition number.
/// </summary>
public class PropertyKycDetailsQueryParameters
{
    /// <summary>
    /// Ward identifier associated with the property.
    /// </summary>
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertyKyc_WardId_Invalid")]
    public int WardId { get; set; }

    /// <summary>
    /// User identifier requesting the property details.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Property number used to locate the property.
    /// </summary>
    [Required(ErrorMessage = "PropertyKyc_PropertyNo_Required")]
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional partition number associated with the property.
    /// </summary>
    public string? PartitionNo { get; set; }
}