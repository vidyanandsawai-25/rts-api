namespace NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;

public class ApartmentDashboardDto
{
    public int SocietyId { get; set; }
    public int TotalProperties { get; set; }
    public int TotalWings { get; set; }
    public int TotalFloors { get; set; }
    public int Assessed { get; set; }
    public int Unassessed { get; set; }
    public decimal AssessedPercentage { get; set; }
    public decimal UnassessedPercentage { get; set; }
    public int TotalAmenities { get; set; }
    public int TotalParking { get; set; }
    public int TotalLifts { get; set; }
    public int InternalSurveyVerified { get; set; }
    public int InternalSurveyPending { get; set; }
    public decimal InternalSurveyVerifiedPercentage { get; set; }
    public decimal InternalSurveyPendingPercentage { get; set; }
    public int InternalSurveyVerifiedAssessed { get; set; }
    public int InternalSurveyVerifiedUnassessed { get; set; }
    public decimal InternalSurveyVerifiedAssessedPercentage { get; set; }
    public decimal InternalSurveyVerifiedUnassessedPercentage { get; set; }
    public int SubmissionComplete { get; set; }
    public int SubmissionPending { get; set; }
    public decimal SubmissionCompletePercentage { get; set; }
    public decimal SubmissionPendingPercentage { get; set; }
    public int SubmissionCompleteAssessed { get; set; }
    public int SubmissionCompleteUnassessed { get; set; }
    public decimal SubmissionCompleteAssessedPercentage { get; set; }
    public decimal SubmissionCompleteUnassessedPercentage { get; set; }
}
