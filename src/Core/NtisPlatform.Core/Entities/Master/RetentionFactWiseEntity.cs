namespace NtisPlatform.Core.Entities;

// <summary>
// Represents retention policy information based on factor ranges and their corresponding values.
// </summary>
public class RetentionFactWiseEntity : BaseEntity
{
    public double? FromFactor { get; set; }
    public double? ToFactor { get; set; }
    public double? FactorValue { get; set; }
}
