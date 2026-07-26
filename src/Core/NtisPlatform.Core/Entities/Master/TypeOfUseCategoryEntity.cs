namespace NtisPlatform.Core.Entities;

public class TypeOfUseCategoryEntity : BaseEntity
{
    public string? TypeOfUseCategoryCode { get; set; }
    public string? TypeOfUseCategoryName { get; set; }
    public ICollection<TypeOfUseEntity> TypeOfUse { get; set; } = new List<TypeOfUseEntity>();
    public ICollection<SubTypeOfUseEntity> SubTypeOfUse { get; set; } = new List<SubTypeOfUseEntity>();
}
