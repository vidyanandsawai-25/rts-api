namespace NtisPlatform.Core.Entities
{
    public class MultilingualResourceEntity : BaseEntity
    {
        public string Resource { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string en_US { get; set; } = string.Empty;
        public string hi_IN { get; set; } = string.Empty;
        public string mr_IN { get; set; } = string.Empty;
        public bool? IsGenerated { get; set; } 
    }
}