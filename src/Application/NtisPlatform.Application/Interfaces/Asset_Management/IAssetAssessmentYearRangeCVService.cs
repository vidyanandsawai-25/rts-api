using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>CRUD service for [AMS].[AssessmentYearRangeMaster].</summary>
public interface IAssetAssessmentYearRangeCVService :
    ICommonCrudService<
        AssetAssessmentYearRangeMasterCVEntity,
        AssetAssessmentYearRangeMasterCVDto,
        CreateAssetAssessmentYearRangeMasterCVDto,
        UpdateAssetAssessmentYearRangeMasterCVDto,
        AssetAssessmentYearRangeMasterCVQueryParameters,
        int>
{
}
