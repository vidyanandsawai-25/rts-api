using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a floor entity manage floor information.
/// </summary>
public class FloorEntity :BaseEntity
{
 
    public string? FloorCode { get; set; }
    public string? Description { get; set; }
 
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }

    public int? FloorGroupId { get; set; }
    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<FloorFactorCVMasterEntity> FloorFactorCVMaster { get; set; } = new List<FloorFactorCVMasterEntity>();
    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();

}


