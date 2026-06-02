using NtisPlatform.Core.Exceptions;

namespace NtisPlatform.Application.Exceptions;

/// <summary>
/// Exception thrown when property data required for CV calculation is not found or invalid
/// </summary>
public class PropertyDataNotFoundException : EntityNotFoundException
{
    public PropertyDataNotFoundException(int propertyId)
        : base("Property", propertyId, "CV_PROPERTY_NOT_FOUND")
    {
    }

    public PropertyDataNotFoundException(int propertyId, string additionalInfo)
        : base("Property", propertyId, "CV_PROPERTY_DATA_INVALID")
    {
        Data["AdditionalInfo"] = additionalInfo;
    }
}

/// <summary>
/// Exception thrown when property details are not found or invalid
/// </summary>
public class PropertyDetailsNotFoundException : EntityNotFoundException
{
    public PropertyDetailsNotFoundException(int propertyId, int propertyDetailsId = 0)
        : base("PropertyDetails", propertyDetailsId == 0 ? $"for Property {propertyId}" : propertyDetailsId, "CV_PROPERTY_DETAILS_NOT_FOUND")
    {
        Data["PropertyId"] = propertyId;
    }
}

/// <summary>
/// Exception thrown when required master data for CV calculation is missing
/// </summary>
public class MasterDataNotFoundException : NtisPlatformException
{
    public MasterDataNotFoundException(string masterDataType, string criteria)
        : base($"{masterDataType} not found for criteria: {criteria}", "CV_MASTER_DATA_NOT_FOUND")
    {
        Data["MasterDataType"] = masterDataType;
        Data["Criteria"] = criteria;
    }
}

/// <summary>
/// Exception thrown when finance year data is invalid or not found
/// </summary>
public class FinanceYearNotFoundException : NtisPlatformException
{
    public FinanceYearNotFoundException(int? financeYear)
        : base(
            financeYear.HasValue 
                ? $"Active finance year {financeYear.Value} not found or is inactive."
                : "No active finance year found in the system.",
            "CV_FINANCE_YEAR_NOT_FOUND")
    {
        if (financeYear.HasValue)
            Data["FinanceYear"] = financeYear.Value;
    }
}

/// <summary>
/// Exception thrown when year range for assessment year is not found
/// </summary>
public class YearRangeNotFoundException : NtisPlatformException
{
    public YearRangeNotFoundException(int assessmentYear, int propertyDetailsId)
        : base($"Year range not found for assessment year {assessmentYear}", "CV_YEAR_RANGE_NOT_FOUND")
    {
        Data["AssessmentYear"] = assessmentYear;
        Data["PropertyDetailsId"] = propertyDetailsId;
    }
}

/// <summary>
/// Exception thrown when rate master data is not found
/// </summary>
public class RateMasterNotFoundException : MasterDataNotFoundException
{
    public RateMasterNotFoundException(int moujaId, string csn, int assessmentYear, int typeOfUseGroupId, int? floorGroupId)
        : base("Rate Master", 
            $"MoujaId: {moujaId}, CSN: {csn}, AssessmentYear: {assessmentYear}, TypeOfUseGroupId: {typeOfUseGroupId}" +
            (floorGroupId.HasValue ? $", FloorGroupId: {floorGroupId}" : ", FloorGroupId: N/A"))
    {
        Data["MoujaId"] = moujaId;
        Data["CSN"] = csn;
        Data["AssessmentYear"] = assessmentYear;
        Data["TypeOfUseGroupId"] = typeOfUseGroupId;
        if (floorGroupId.HasValue)
            Data["FloorGroupId"] = floorGroupId.Value;
    }
}

/// <summary>
/// Exception thrown when CSN details mapping to rate master is not found.
/// This occurs when no CSNDetails rows match the given MoujaId/CSN combination.
/// </summary>
public class CSNRateMappingNotFoundException : MasterDataNotFoundException
{
    public CSNRateMappingNotFoundException(int moujaId, string csn)
        : base("CSN Rate Mapping", $"MoujaId: {moujaId}, CSN: {csn}")
    {
        Data["MoujaId"] = moujaId;
        Data["CSN"] = csn;
    }
}

/// <summary>
/// Exception thrown when tax percentage data is not configured
/// </summary>
public class TaxPercentageNotFoundException : MasterDataNotFoundException
{
    public TaxPercentageNotFoundException(int typeOfUseId, string typeOfUseDescription, int assessmentYear, int yearRangeId)
        : base("Tax Percentage", 
            $"TypeOfUse: {typeOfUseDescription} (ID: {typeOfUseId}), AssessmentYear: {assessmentYear}, YearRangeId: {yearRangeId}")
    {
        Data["TypeOfUseId"] = typeOfUseId;
        Data["TypeOfUseDescription"] = typeOfUseDescription;
        Data["AssessmentYear"] = assessmentYear;
        Data["YearRangeId"] = yearRangeId;
    }
}

/// <summary>
/// Exception thrown when property data contains invalid values
/// </summary>
public class InvalidPropertyDataException : NtisPlatformException
{
    public InvalidPropertyDataException(string fieldName, object? value, int propertyDetailsId)
        : base($"Invalid {fieldName} value: '{value}' for PropertyDetails ID {propertyDetailsId}", "CV_INVALID_PROPERTY_DATA")
    {
        Data["FieldName"] = fieldName;
        Data["InvalidValue"] = value?.ToString() ?? "null";
        Data["PropertyDetailsId"] = propertyDetailsId;
    }
}

/// <summary>
/// Exception thrown when type of use group data is missing
/// </summary>
public class TypeOfUseGroupNotFoundException : NtisPlatformException
{
    public TypeOfUseGroupNotFoundException(int propertyDetailsId, int typeOfUseId)
        : base($"TypeOfUseGroup not found for PropertyDetails {propertyDetailsId}, TypeOfUseId {typeOfUseId}", "CV_TYPE_OF_USE_GROUP_NOT_FOUND")
    {
        Data["PropertyDetailsId"] = propertyDetailsId;
        Data["TypeOfUseId"] = typeOfUseId;
    }
}

/// <summary>
/// Exception thrown when floor group is required but not found
/// </summary>
public class FloorGroupNotFoundException : NtisPlatformException
{
    public FloorGroupNotFoundException(int propertyDetailsId)
        : base($"FloorGroup not found for floor-wise rate PropertyDetails {propertyDetailsId}", "CV_FLOOR_GROUP_NOT_FOUND")
    {
        Data["PropertyDetailsId"] = propertyDetailsId;
    }
}
