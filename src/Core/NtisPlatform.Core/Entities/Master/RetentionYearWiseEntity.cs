
namespace NtisPlatform.Core.Entities;

    /// <summary>
    /// Represents retention policy information based on Year ranges and their corresponding values.
    /// </summary>
    public class RetentionYearWiseEntity : CommonBaseEntity
    {
        public int ID { get; set; }
        public int? FromYear { get; set; }
        public int? ToYear { get; set; }
        public double? FactorValue { get; set; }    
    }
