using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class SubTypeOfUseEntity : BaseEntity
{
  
    public string? Description { get; set; } = string.Empty;
    public int TypeOfUseId { get; set; } 
    public int? SearchSequence { get; set; }
    public int? TypeOfUseCategoryId { get; set; }
    public virtual TypeOfUseEntity? TypeOfUse { get; set; }
    public virtual TypeOfUseCategoryEntity? TypeOfUseCategory { get; set; }
    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();
    public ICollection<UseFactorCVMasterEntity> UseFactorCVMaster { get; set; } = new List<UseFactorCVMasterEntity>();
}

