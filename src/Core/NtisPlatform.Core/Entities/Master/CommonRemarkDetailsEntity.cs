namespace NtisPlatform.Core.Entities.Master
{
    public class CommonRemarkDetailsEntity : BaseEntity
    {
        public int RemarkTypeId { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
