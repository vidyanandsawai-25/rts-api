using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

public class MoujaEntity : BaseEntity
{
    public int Id { get; set; } = 0;

    public int Year { get; set; } = 0;

    [MaxLength(50)]
    public string MoujaName { get; set; } = string.Empty;

}
