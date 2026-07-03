using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property details in the PTIS system
/// </summary>
public class PropertyDetailsEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }

    public int? FloorId { get; set; }

    public int? SubFloorId { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }

    public int? ConstructionTypeId { get; set; }

    public int TypeOfUseId { get; set; }

    public double? CarpetAreaSqMeter { get; set; }

    public double? CarpetAreaSqFeet { get; set; }

    public double? BuiltupAreaSqMeter { get; set; }

    public double? BuiltupAreaSqFeet { get; set; }

    public int? NoOfRooms { get; set; }

    public bool? IsRenter { get; set; }

    public int? SubTypeOfUseId { get; set; }

    public bool? IsTaxable { get; set; }
    public bool? IsOpenPlot { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation Properties
    /// <summary>
    /// Collection of renter master records associated with this property detail
    /// </summary>
    public virtual ICollection<RenterMastEntity> Renters { get; set; } = new List<RenterMastEntity>();


    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity? Property { get; set; }

          
    public virtual FloorEntity? Floor { get; set; }

    [ForeignKey(nameof(SubFloorId))]         
    public virtual SubFloorEntity? SubFloor { get; set; }

   
    public virtual ConstructionTypeEntity? ConstructionType { get; set; }

    [ForeignKey(nameof(TypeOfUseId))]
    public virtual TypeOfUseEntity? TypeOfUse { get; set; }

    [ForeignKey(nameof(SubTypeOfUseId))]
    public virtual SubTypeOfUseEntity? SubTypeOfUse { get; set; }

    /// <summary>
    /// Collection of property tax calculation CV results associated with this property detail
    /// </summary>
    public virtual ICollection<PropertyTaxCalculationCVResultsEntity> PropertyTaxCalculationCVResults { get; set; } = new List<PropertyTaxCalculationCVResultsEntity>();

    /// <summary>
    /// Collection of property tax calculation RV results associated with this property detail
    /// </summary>
    public virtual ICollection<PropertyTaxCalculationRVResultsEntity> PropertyTaxCalculationRVResults { get; set; } = new List<PropertyTaxCalculationRVResultsEntity>();

    /// <summary>
    /// Collection of property tax calculation Section 129 results associated with this property detail
    /// </summary>
    public virtual ICollection<PropertyTaxCalculationSection129ResultsEntity> PropertyTaxCalculationSection129Results { get; set; } = new List<PropertyTaxCalculationSection129ResultsEntity>();


    /// <summary>
    /// Collection of renter details associated with this property detail
    /// </summary>
    public virtual ICollection<RenterDetailEntity> RenterDetails { get; set; } = new List<RenterDetailEntity>();

    /// <summary>
    /// Collection of room-wise submission details associated with this property detail
    /// </summary>
    public virtual ICollection<RoomWiseSubmissionDetailsEntity> RoomWiseSubmissionDetails { get; set; } = new List<RoomWiseSubmissionDetailsEntity>();
    /// <summary>
    /// Collection of Property Occupancy Details associated with this property detail
    /// </summary>
    public virtual ICollection<PropertyOccupancyDetailsEntity> PropertyOccupancyDetails { get; set; } = new List<PropertyOccupancyDetailsEntity>();



}
