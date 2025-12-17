using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

public class PTISConstructionTypeMasterEntity : BaseEntity
{
    public string ConstructionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreatedBy { get; set; } = 0;
    public int UpdatedBy { get; set; } = 0;

}
public class PTISFloorMasterEntity : BaseEntity
{
    public string? FloorID { get; set; }
    public string? Description { get; set; }

    public int? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

}

