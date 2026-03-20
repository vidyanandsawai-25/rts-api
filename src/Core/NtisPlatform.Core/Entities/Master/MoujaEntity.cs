using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

public class MoujaEntity : BaseEntity
{
    public int MoujaId { get; set; } = 0;

    public int Year { get; set; } = 0;

    public string MoujaName { get; set; } = string.Empty;

}
