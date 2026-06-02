using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class TypeOfUseEntity : BaseEntity
{
     public string? TypeOfUseCode { get; set; }
     public string? Description { get; set; }
     public string? Type { get; set; }
     public int TypeOfUseGroupId { get; set; }
     public int TypeOfUseGroupCVId { get; set; }

     public int? SearchSequence { get; set; }   
     public virtual TypeOfUseGroupEntity? TypeOfUseGroup { get; set; }
     public virtual TypeOfUseGroupCVEntity? TypeOfUseGroupCV { get; set; }

    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();
    public ICollection<UseFactorCVMasterEntity> UseFactorCVMaster { get; set; } = new List<UseFactorCVMasterEntity>();
    public ICollection<ParkingTypeMasterEntity> ParkingTypeMaster { get; set; } = new List<ParkingTypeMasterEntity>();
    public ICollection<TaxPercentageMasterCVEntity> TaxPercentageMasterCV { get; set; } = new List<TaxPercentageMasterCVEntity>();
    public ICollection<PropertyDescriptionAndTypeOfUseValidationEntity> PropertyDescriptionAndTypeOfUseValidation { get; set; } = new List<PropertyDescriptionAndTypeOfUseValidationEntity>();
    public ICollection<SubTypeOfUseEntity> SubTypeOfUse { get; set; } = new List<SubTypeOfUseEntity>();
    public ICollection<TaxPercentageMasterRVEntity> TaxPercentageMasterRV { get; set; } = new List<TaxPercentageMasterRVEntity>();
}

