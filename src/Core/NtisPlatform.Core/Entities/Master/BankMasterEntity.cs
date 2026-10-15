namespace NtisPlatform.Core.Entities.Master
{
    public class BankMasterEntity : BaseEntity
    {
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string? IFSCCode { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
    }
}
