namespace NtisPlatform.Core.Entities.Master
{
    public class YearMasterEntity : BaseEntity
    {     
        
        public int Year { get; set; }
        public string? YearCode { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
