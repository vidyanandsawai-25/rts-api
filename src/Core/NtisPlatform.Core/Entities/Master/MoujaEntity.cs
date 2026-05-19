using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

public class MoujaEntity : BaseEntity
{
    public string MoujaNo { get; set; } = string.Empty;

    public string MoujaName { get; set; } = string.Empty;

}
