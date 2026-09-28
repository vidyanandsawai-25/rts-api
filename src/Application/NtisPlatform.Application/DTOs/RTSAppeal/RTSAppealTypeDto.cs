using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Data transfer object representing a statutory ground / category for an RTS appeal.
/// Inherits from BaseDtos for standardized identity and audit handling.
/// </summary>
public class RTSAppealTypeDto : BaseDtos
{
    public string Code { get; set; } = string.Empty;
    public string AppealTypeName { get; set; } = string.Empty;
}

/// <summary>
/// Payload for creating a new RTS appeal type master entry.
/// </summary>
public class CreateRTSAppealTypeDto : CreateBaseDtos
{
    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string AppealTypeName { get; set; } = string.Empty;
}

/// <summary>
/// Payload for updating an existing RTS appeal type master entry.
/// </summary>
public class UpdateRTSAppealTypeDto : UpdateBaseDtos
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string AppealTypeName { get; set; } = string.Empty;
}
