using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Core.Entities;

public class TypeOfUseGroupEntity : BaseEntity
{
    public int Id { get; set; }
    public string TypeOfUseGroupCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? GroupIcon { get; set; }

}

