using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Generation Tab - includes joined data from multiple tables
/// </summary>
public class BuildingGenerateDetailsDto
{
    [Required] 
    public int WardId { get; set; }
    [Required]
    public string? PropertyNo { get; set; } = string.Empty;
    [Required] 
    public int WingId { get; set; }
    [Required]
    public string? FromFloor { get; set; } = string.Empty;
    [Required]
    public string? ToFloor { get; set; } = string.Empty;
    [Required]
    public int NoOfFlatOnOneFloor { get; set; }

    [Required]
    public int FlatStart { get; set; }

    [Required]
    public int IncrementedBy { get; set; }
    public string? Prifix { get; set; } = string.Empty;

    [Required]
    public string GenerationType { get; set; } = string.Empty;
}

public class BuildingGenerateStructureDto
{
    public int WardId { get; set; }
    public string? PropertyNo { get; set; } = string.Empty;
    public int WingId { get; set; }
    public int RowNo { get; set; }
    public int FloorNo { get; set; }
    public string floorCode { get; set; } = string.Empty;
    public int PropertyFloorId{ get; set; }
    public int UnitNo { get; set; }
    public string? FlatNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; } = string.Empty;
    public string? GenerationType { get; set; } = string.Empty;
}

public class BuildingListDto
{
    public int PropertyId { get; set; }
    public string? WardNo { get; set; } = string.Empty;
    public string? PropertyNo { get; set; } = string.Empty;
    public string? CatPropertyCategoryName { get; set; } = string.Empty;
    public string? PartitionNo { get; set; } = string.Empty;
}

public class MaxPartitionNoDto
{
    public string? WardNo { get; set; } = string.Empty;
    public string? PropertyNo { get; set; } = string.Empty;
    public string? Category { get; set; } = string.Empty;
    public string? MaxPartitionNo { get; set; } = string.Empty;
}
