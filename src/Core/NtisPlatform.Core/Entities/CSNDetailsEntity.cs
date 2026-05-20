namespace NtisPlatform.Core.Entities
{
    public class CSNDetailsEntity : BaseEntity
    {
        public int RateMasterCVId { get; set; }
        public string CSN { get; set; } = string.Empty;

        // NAVIGATION PROPERTY
        public virtual RateMasterForCVEntity RateMasterForCV { get; set; } = null!;

    }
}
