namespace NtisPlatform.Core.Models;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for single Property Details Old record - represents one floor's historical information
/// Used in the Historical Floor Information section of the Old Details tab
/// </summary>
public class PropertyDetailsOldDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    /// <summary>
    /// Floor ID (FloorMaster.Id)
    /// </summary>
    public int? OldFloorId { get; set; }

    /// <summary>
    /// Floor description for display (joined from FloorMaster)
    /// </summary>
    public string? FloorDescription { get; set; }

    /// <summary>
    /// Sub Floor ID (SubFloorMaster.Id)
    /// </summary>
    public int? OldSubFloorId { get; set; }

    /// <summary>
    /// Sub Floor description for display (joined from SubFloorMaster)
    /// </summary>
    public string? SubFloorDescription { get; set; }

    /// <summary>
    /// Construction Year as string (e.g., "2020")
    /// </summary>
    public string? OldConstructionYear { get; set; }

    /// <summary>
    /// Year value parsed from OldConstructionYear string
    /// </summary>
    public int? ConstructionYearValue { get; set; }

    /// <summary>
    /// Assessment Year as string (e.g., "2020")
    /// </summary>
    public string? OldAssessmentYear { get; set; }

    /// <summary>
    /// Year value parsed from OldAssessmentYear string
    /// </summary>
    public int? AssessmentYearValue { get; set; }

    /// <summary>
    /// Construction Type ID (ConstructionTypeMaster.Id)
    /// </summary>
    public int? OldConstructionTypeId { get; set; }

    /// <summary>
    /// Construction type description for display (joined from ConstructionTypeMaster)
    /// </summary>
    public string? ConstructionTypeDescription { get; set; }

    /// <summary>
    /// Type of Use ID (TypeOfUseMaster.Id)
    /// </summary>
    public int? OldTypeOfUseId { get; set; }

    /// <summary>
    /// Type of use description for display (joined from TypeOfUseMaster)
    /// </summary>
    public string? TypeOfUseDescription { get; set; }

    /// <summary>
    /// Sub Type of Use ID (SubTypeOfUseMaster.Id)
    /// </summary>
    public int? OldSubTypeOfUseId { get; set; }

    /// <summary>
    /// Sub Type of use description for display (joined from SubTypeOfUseMaster)
    /// </summary>
    public string? SubTypeOfUseDescription { get; set; }

    /// <summary>
    /// Carpet area in square meters
    /// </summary>
    public double? OldCarpetAreaSqMeter { get; set; }

    /// <summary>
    /// Carpet area in square feet
    /// </summary>
    public double? OldCarpetAreaSqFeet { get; set; }

    /// <summary>
    /// Builtup area in square meters
    /// </summary>
    public double? OldBuiltupAreaSqMeter { get; set; }

    /// <summary>
    /// Builtup area in square feet
    /// </summary>
    public double? OldBuiltupAreaSqFeet { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

/// <summary>
/// Response DTO for GET /api/Property/{propertyId}/floor-details-old
/// Returns a list of historical floor details for a property
/// </summary>
public class PropertyDetailsOldListDto
{
    public int PropertyId { get; set; }
    public List<PropertyDetailsOldDto> FloorDetails { get; set; } = new();
}

/// <summary>
/// Request DTO for adding a new Property Details Old record via POST
/// Used for adding a single floor record
/// </summary>
public class AddPropertyDetailsOldDto
{
    public int? OldFloorId { get; set; }
    public int? OldSubFloorId { get; set; }
    public string? OldConstructionYear { get; set; }
    public string? OldAssessmentYear { get; set; }
    public int? OldConstructionTypeId { get; set; }
    public int? OldTypeOfUseId { get; set; }
    public int? OldSubTypeOfUseId { get; set; }
    public double? OldCarpetAreaSqMeter { get; set; }
    public double? OldCarpetAreaSqFeet { get; set; }
    public double? OldBuiltupAreaSqMeter { get; set; }
    public double? OldBuiltupAreaSqFeet { get; set; }
}

/// <summary>
/// Request DTO for updating a single existing Property Details Old record
/// Used for updating one floor record via the update endpoint
/// </summary>
public class UpdatePropertyDetailsOldDto
{
    public int? OldFloorId { get; set; }
    public int? OldSubFloorId { get; set; }
    public string? OldConstructionYear { get; set; }
    public string? OldAssessmentYear { get; set; }
    public int? OldConstructionTypeId { get; set; }
    public int? OldTypeOfUseId { get; set; }
    public int? OldSubTypeOfUseId { get; set; }
    public double? OldCarpetAreaSqMeter { get; set; }
    public double? OldCarpetAreaSqFeet { get; set; }
    public double? OldBuiltupAreaSqMeter { get; set; }
    public double? OldBuiltupAreaSqFeet { get; set; }
}
