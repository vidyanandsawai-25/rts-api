using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

public class ActiveTaxesEntity : CommonBaseEntity
{
    public int TaxNameID { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? TaxName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? TaxNameAlias { get; set; }

    public int? TaxNameOrder { get; set; }
    public bool? ActiveTaxHeadsOnly { get; set; }
    public int? DisplayOrder { get; set; }
}
