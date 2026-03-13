using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Core.Entities;

public class TypeOfUseGroupEntity : BaseEntity
{
    public string TypeOfUseGroupID { get; set; } = "";
    public string? GroupNameEnglish { get; set; }
    public string GroupName { get; set; } = "";
    public string? GroupIcon { get; set; }

}

