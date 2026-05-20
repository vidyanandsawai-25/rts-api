using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Core.Entities;

public class TypeOfUseGroupEntity : BaseEntity
{
    public string TypeOfUseGroupCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? GroupIcon { get; set; }

    public bool IsFloorWiseRateApplicable { get; set; }// use for cv Calculation

    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<TypeOfUseEntity> TypeOfUse { get; set; } = new List<TypeOfUseEntity>();

}

