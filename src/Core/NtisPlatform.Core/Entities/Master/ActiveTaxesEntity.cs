using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

public class ActiveTaxesEntity : BaseEntity
{    [Column(TypeName = "nvarchar(200)")]
    public string? TaxName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? TaxNameAlias { get; set; }

    public int? DisplayOrder { get; set; }

    public bool TaxOnUnit { get; set; }
}
