using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents the validation relationship between property types and type of use in the PTIS system
/// </summary>
[Table("PropertyDescriptionAndTypeOfUseValidation", Schema = "PTIS")]
public class PropertyDescriptionAndTypeOfUseValidationEntity : BaseEntity
{


    public int PropertyTypeId { get; set; }
    
    public int TypeOfUseId { get; set; }
    public virtual TypeOfUseEntity? TypeOfUse { get; set; }

    public virtual PropertyTypeMasterEntity? PropertyTypeMaster { get; set; }

}
