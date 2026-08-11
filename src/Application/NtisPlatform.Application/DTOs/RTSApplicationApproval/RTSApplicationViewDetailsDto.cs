namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;


/// <summary>
/// view applicationDetails Api Select DTOs admin And citizen screen 
/// </summary>
public class RTSApplicationViewDetailsDto
{
    public List<ApplicationDocumentDto> Documents { get; set; } = new();
    public List<ApplicationFieldValueDto> ApplicationDetails { get; set; } = new();
}

public class ApplicationDocumentDto
{
    public int FieldDefinitionId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public Guid? DocumentGuid { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUploaded { get; set; }
}

public class ApplicationFieldValueDto
{
    public int FieldDefinitionId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? FieldLabelLocal { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string? FieldGroup { get; set; }
    public string? Value { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
}


    /// <summary>
    /// Application Approval Stages 
    /// </summary>

    public class ApplicationApprovalStageDetailsDto
    {
        public int? TotalApprovalStages { get; set; }
        public int CompletedStages { get; set; }
        public List<ApplicationApprovalStageDto> ApprovalStages { get; set; } = new();
    }

    public class ApplicationApprovalStageDto
    {
        public int ApprovalFlowStageId { get; set; }
        public int StageOrder { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? Remark { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsCurrentStage { get; set; }
        public DateTime? CreatedDate { get; set; }
}
