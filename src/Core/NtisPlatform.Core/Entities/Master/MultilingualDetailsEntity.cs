namespace NtisPlatform.Core.Entities
{
    public class MultilingualDetailsEntity : BaseEntity
    {
        public int Id { get; set; }
        public string? Resource { get; set; } = string.Empty;
        public string? Key { get; set; } = string.Empty;
        public string? Culture { get; set; } = string.Empty;
        public string? Value { get; set; } = string.Empty;

    }
}