using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.AapleSarkar;

public class AapleSarkarCitizenApplicationsRequestDto
{
    public string? CitizenUserId { get; set; }
    public string? SearchText { get; set; }
    public string? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AapleSarkarCitizenApplicationItemDto
{
    public string ApplicationNo { get; set; } = string.Empty;
    public string AapleSarkarTrackId { get; set; } = string.Empty;
    public int RtsServiceId { get; set; }
    public int MahaITServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceNameMr { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? CreatedDate { get; set; }
    public Guid? IssuedCertificateGuid { get; set; }
    public string? CertificateUrl { get; set; }
    public string? TrackingUrl { get; set; }
}

public class AapleSarkarCitizenApplicationsResponseDto
{
    public bool Status { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<AapleSarkarCitizenApplicationItemDto> Data { get; set; } = new();
}
