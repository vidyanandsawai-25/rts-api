using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// Data Transfer Object representing the request payload to split an existing property or partition.
/// Defines the core validation rules and configuration flags required to accurately generate new property branches.
/// </summary>
public class PropertySplitCreateDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the Ward associated with the property.
    /// Used to locate the base property and ensure regional constraints.
    /// </summary>
    [Required(ErrorMessage = "WARD_ID_REQUIRED")]
    [Range(1, int.MaxValue, ErrorMessage = "WARD_ID_GREATER_THAN_ZERO")]
    public int WardId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user initiating the split request.
    /// Used for authorization validation to ensure the user has rights in the specified Ward.
    /// </summary>
    [Required(ErrorMessage = "USER_ID_REQUIRED")]
    [Range(1, int.MaxValue, ErrorMessage = "USER_ID_GREATER_THAN_ZERO")]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the base property number that acts as the root for this split operation.
    /// </summary>
    [Required(ErrorMessage = "PROPERTY_NO_REQUIRED")]
    [StringLength(10, ErrorMessage = "PROPERTY_NO_EXCEEDS_MAX_LENGTH")]
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of consecutive splits to generate in a single transaction.
    /// </summary>
    [Required(ErrorMessage = "NO_OF_SPLITS_REQUIRED")]
    [Range(1, 100, ErrorMessage = "NO_OF_SPLITS_GREATER_THAN_ZERO_OR_LESS_THAN_100")]
    public int NoOfSplit { get; set; }

    /// <summary>
    /// Specifies the type of split logic to apply:
    /// <c>false</c> represents a Property Split (e.g., 100 becomes 100A, 100B).
    /// <c>true</c> represents a Partition Split (e.g., Partition 1 becomes 1A, 1B).
    /// </summary>
    [Required]
    public bool IsPartitionProperty { get; set; }

    private string? _partitionNo;
    /// <summary>
    /// Gets or sets the specific partition number to split from. 
    /// Required only when <see cref="IsPartitionProperty"/> is set to <c>true</c>.
    /// </summary>
    [StringLength(10, ErrorMessage = "PARTITION_NO_EXCEEDS_MAX_LENGTH")]
    public string? PartitionNo
    {
        get => _partitionNo;
        set => _partitionNo = value?.Equals("null", StringComparison.OrdinalIgnoreCase) == true ? null : value;
    }

    /// <summary>
    /// Indicates whether to clone the full comprehensive data from the parent property (<c>true</c>) 
    /// or generate a minimal, basic property record (<c>false</c>).
    /// </summary>
    [Required]
    public bool IsMainPropertyDataAttach { get; set; } = true;

    /// <summary>
    /// Gets or sets the activation status of the newly generated properties.
    /// </summary>
    [Required]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the user identifier representing the original creator of these split records.
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// Data Transfer Object representing the resulting details of a successfully executed property split operation.
/// Includes the hierarchical relationships and generated identifiers.
/// </summary>
public class PropertySpiltResponseDto
{
    /// <summary>
    /// Gets or sets the newly generated, alphabetically sequenced property number (e.g., "100A").
    /// </summary>
    public string GeneratedPropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original root property number that served as the base for the split.
    /// </summary>
    public string ParentPropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the newly generated, alphabetically sequenced partition number (e.g., "1A").
    /// Null if the operation was exclusively a property split without partitions.
    /// </summary>
    public string? GeneratedPartitionNo { get; set; }

    /// <summary>
    /// Gets or sets the original partition number that served as the base for the partition split.
    /// </summary>
    public string? ParentPartitionNo { get; set; }

    /// <summary>
    /// Indicates whether the generated record is fundamentally classified as a split property.
    /// </summary>
    public bool IsSplit { get; set; }

    /// <summary>
    /// Indicates whether the split occurred at the partition level.
    /// </summary>
    public bool IsPartitionProperty { get; set; }

    /// <summary>
    /// Gets or sets the primary database identifier assigned to the newly created property record.
    /// </summary>
    public int PropertyId { get; set; }
}

/// <summary>
/// Data Transfer Object representing the final result of a property split operation,
/// categorizing records into newly created ones and previously existing (skipped) ones.
/// </summary>
public class PropertySplitResultDto
{
    /// <summary>
    /// A list of split properties that were already existing in the system (skipped).
    /// </summary>
    public List<PropertySpiltResponseDto> Skipped { get; set; } = new();

    /// <summary>
    /// A list of split properties that were successfully generated and inserted.
    /// </summary>
    public List<PropertySpiltResponseDto> Created { get; set; } = new();
}

/// <summary>
/// Data Transfer Object representing the request payload to view properties.
/// </summary>
public class PropertyGetDto
{
    [Required(ErrorMessage = "WARD_ID_REQUIRED")]
    [Range(1, int.MaxValue, ErrorMessage = "WARD_ID_GREATER_THAN_ZERO")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "PROPERTY_NO_REQUIRED")]
    public string PropertyNo { get; set; } = string.Empty;

    private string? _partitionNo;
    public string? PartitionNo
    {
        get => _partitionNo;
        set => _partitionNo = value?.Equals("null", StringComparison.OrdinalIgnoreCase) == true ? null : value;
    }
}

/// <summary>
/// Request DTO for looking up property by WardId, PropertyNo, and optional PartitionNo
/// Used by POST /kyc-details and POST /society-details endpoints
/// </summary>
public class PropertyLookupRequestDto
{
    /// <summary>
    /// Ward ID (required)
    /// </summary>
    public int WardId { get; set; }

    /// <summary>
    /// User ID (required)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Property Number (required)
    /// </summary>
    public string PropertyNo { get; set; } = string.Empty;

    private string? _partitionNo;
    /// <summary>
    /// Partition Number (optional)
    /// </summary>
    public string? PartitionNo
    {
        get => _partitionNo;
        set => _partitionNo = value?.Equals("null", StringComparison.OrdinalIgnoreCase) == true ? null : value;
    }
}

public class PropertyHierarchyResponseDto
{
    public int PropertyId { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
    public string? PropCategoryDesc { get; set; }
    public string? OwnerName { get; set; }
    public List<PropertySplitHierarchyDto> Splits { get; set; } = new();
    public List<PropertyPartitionDetailDto> Partitions { get; set; } = new();
}

public class PropertySplitHierarchyDto
{
    public int PropertyId { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
    public string? PropCategoryDesc { get; set; }
    public string? OwnerName { get; set; }
    public List<PropertyPartitionDetailDto> Partitions { get; set; } = new();
}

public class PropertyPartitionDetailDto
{
    public int PropertyId { get; set; }
    public string PartitionNo { get; set; } = string.Empty;
    public string? PropCategoryDesc { get; set; }
    public string? OwnerName { get; set; }
}
