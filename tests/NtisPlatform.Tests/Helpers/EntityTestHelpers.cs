using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Helpers;

/// <summary>
/// Helper methods for creating test entities with internal constructors
/// </summary>
public static class EntityTestHelpers
{
    /// <summary>
    /// Creates a minimal DocumentEntity for testing
    /// </summary>
    public static DocumentEntity CreateDocumentEntity(
        int id = 1,
        Guid? documentGuid = null,
        int uploadedByUserId = 1,
        string fileName = "test.pdf",
        string originalFileName = "test.pdf",
        string fileExtension = ".pdf",
        string mimeType = "application/pdf",
        long fileSizeBytes = 1024,
        string storagePath = "/uploads/test.pdf",
        string? storageProvider = null,
        string? documentType = null,
        string? uploadStatusCode = null,
        int downloadCount = 0)
    {
        var entity = new DocumentEntity(
            documentGuid: documentGuid ?? Guid.NewGuid(),
            uploadedByUserId: uploadedByUserId,
            fileName: fileName,
            originalFileName: originalFileName,
            fileExtension: fileExtension,
            mimeType: mimeType,
            fileSizeBytes: fileSizeBytes,
            storagePath: storagePath,
            storageProvider: storageProvider,
            documentType: documentType,
            uploadStatusCode: uploadStatusCode,
            downloadCount: downloadCount);

        // Use reflection to set Id for testing purposes
        var idProperty = typeof(DocumentEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);

        return entity;
    }

    /// <summary>
    /// Creates a minimal DocumentBindingEntity for testing
    /// </summary>
    public static DocumentBindingEntity CreateDocumentBindingEntity(
        int documentId = 1,
        int departmentId = 1,
        int moduleId = 1,
        string referenceTableName = "TestTable",
        string referencePropertyName = "Id",
        int? referenceTableId = 1,
        Guid? referenceTableIdGuid = null,
        string? bindingPurpose = null,
        bool isPrimaryDocument = false)
    {
        return new DocumentBindingEntity(
            documentId: documentId,
            departmentId: departmentId,
            moduleId: moduleId,
            referenceTableName: referenceTableName,
            referencePropertyName: referencePropertyName,
            referenceTableId: referenceTableId,
            referenceTableIdGuid: referenceTableIdGuid,
            bindingPurpose: bindingPurpose,
            isPrimaryDocument: isPrimaryDocument);
    }

    /// <summary>
    /// Creates a minimal PropertyCertificateEntity for testing
    /// </summary>
    public static PropertyCertificateEntity CreatePropertyCertificateEntity(
        int propertyId = 1,
        int certificateTypeId = 1,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? documentBindingId = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        var entity = new PropertyCertificateEntity(
            propertyId: propertyId,
            certificateTypeId: certificateTypeId,
            certificateNo: certificateNo,
            issueDate: issueDate,
            documentBindingId: documentBindingId,
            markedForDeletion: markedForDeletion,
            markedForDeletionDate: markedForDeletionDate);

        entity.IsActive = !markedForDeletion;
        return entity;
    }

    /// <summary>
    /// Creates a PropertyEntity for testing
    /// </summary>
    public static PropertyEntity CreatePropertyEntity(int id = 1)
    {
        var entity = new PropertyEntity
        {
            PropertyNo = $"PROP-{id:D6}",
            PropertyTypeId = 1,
            TaxZoneId = 1,
            WardId = 1,
            MoujaId = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var idProperty = typeof(PropertyEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);

        return entity;
    }

    /// <summary>
    /// Creates a PropertyCertificateTypeMasterEntity for testing
    /// </summary>
    public static PropertyCertificateTypeMasterEntity CreatePropertyCertificateTypeMasterEntity(int id = 1)
    {
        var entity = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = $"Certificate Type {id}",
            DisplayOrder = id,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var idProperty = typeof(PropertyCertificateTypeMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);

        return entity;
    }

    /// <summary>
    /// Creates entities required for testing - placeholder implementations
    /// </summary>
    public static AssessmentYearRangeCVEntity CreateAssessmentYearRangeCVEntity(int id = 1)
    {
        var entity = new AssessmentYearRangeCVEntity();
        var idProperty = typeof(AssessmentYearRangeCVEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static AgeFactorCVMasterEntity CreateAgeFactorCVMasterEntity(int id = 1, int yearRangeCVId = 1, int constructionTypeId = 1)
    {
        var entity = new AgeFactorCVMasterEntity();
        var idProperty = typeof(AgeFactorCVMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.YearRangeCVId = yearRangeCVId;
        entity.ConstructionTypeId = constructionTypeId;
        entity.IsActive = true;
        return entity;
    }

    public static SubFloorEntity CreateSubFloorEntity(int id = 1)
    {
        var entity = new SubFloorEntity();
        var idProperty = typeof(SubFloorEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static PropertyDetailsEntity CreatePropertyDetailsEntity(int id = 1)
    {
        var entity = new PropertyDetailsEntity();
        var idProperty = typeof(PropertyDetailsEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static ConstructionTypeEntity CreateConstructionTypeEntity(int id = 1)
    {
        var entity = new ConstructionTypeEntity
        {
            ConstructionCode = $"CT{id:D3}",
            Description = $"Construction Type {id}",
            IsActive = true
        };
        var idProperty = typeof(ConstructionTypeEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static RateEntity CreateRateEntity(int id = 1)
    {
        var entity = new RateEntity();
        var idProperty = typeof(RateEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static TaxZoneEntity CreateTaxZoneEntity(int id = 1)
    {
        var entity = new TaxZoneEntity
        {
            TaxZoneNo = $"TZ-{id:D4}",
            Remark = $"Test Tax Zone {id}",
            IsActive = true
        };
        var idProperty = typeof(TaxZoneEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static FloorEntity CreateFloorEntity(int id = 1)
    {
        var entity = new FloorEntity
        {
            FloorCode = $"FL{id:D3}",
            Description = $"Floor {id}",
            IsActive = true
        };
        var idProperty = typeof(FloorEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static FloorFactorCVMasterEntity CreateFloorFactorCVMasterEntity(int id = 1, int floorId = 1, int yearRangeCVId = 1)
    {
        var entity = new FloorFactorCVMasterEntity();
        var idProperty = typeof(FloorFactorCVMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.FloorId = floorId;
        entity.YearRangeCVId = yearRangeCVId;
        entity.IsActive = true;
        return entity;
    }

    public static AssessmentYearRangeEntity CreateAssessmentYearRangeEntity(int id = 1)
    {
        var entity = new AssessmentYearRangeEntity();
        var idProperty = typeof(AssessmentYearRangeEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static DepreciationMasterEntity CreateDepreciationEntity(int id = 1)
    {
        var entity = new DepreciationMasterEntity();
        var idProperty = typeof(DepreciationMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static RateSectionEntity CreateRateSectionEntity(int id = 1)
    {
        var entity = new RateSectionEntity
        {
            Description = $"Rate Section {id}",
            IsActive = true
        };
        var idProperty = typeof(RateSectionEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static RateSectionDetailsEntity CreateRateSectionDetailsEntity(int id = 1)
    {
        var entity = new RateSectionDetailsEntity();
        var idProperty = typeof(RateSectionDetailsEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static WardEntity CreateWardEntity(int id = 1)
    {
        var entity = new WardEntity
        {
            WardNo = $"W{id:D3}",
            ZoneId = 1,
            Description = $"Ward {id}",
            IsActive = true
        };
        var idProperty = typeof(WardEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static BlockMasterEntity CreateBlockMasterEntity(int id = 1)
    {
        var entity = new BlockMasterEntity
        {
            WardId = 1,
            BlockNo = $"BLK{id:D4}",
            IsActive = true
        };
        var idProperty = typeof(BlockMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static ZoneEntity CreateZoneEntity(int id = 1)
    {
        var entity = new ZoneEntity
        {
            ZoneNo = $"Z{id:D3}",
            Description = $"Zone {id}",
            IsActive = true
        };
        var idProperty = typeof(ZoneEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static TypeOfUseGroupEntity CreateTypeOfUseGroupEntity(int id = 1)
    {
        var entity = new TypeOfUseGroupEntity
        {
            TypeOfUseGroupCode = $"TOUG{id:D3}",
            GroupName = $"Use Group {id}",
            IsActive = true
        };
        var idProperty = typeof(TypeOfUseGroupEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static TypeOfUseEntity CreateTypeOfUseEntity(int id = 1)
    {
        var entity = new TypeOfUseEntity
        {
            TypeOfUseCode = $"TOU{id:D3}",
            Description = $"Type of Use {id}",
            Type = $"Type{id}",
            TypeOfUseGroupId = 1,
            IsActive = true
        };
        var idProperty = typeof(TypeOfUseEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static ParkingTypeMasterEntity CreateParkingTypeMasterEntity(int id = 1)
    {
        var entity = new ParkingTypeMasterEntity();
        var idProperty = typeof(ParkingTypeMasterEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static SubTypeOfUseEntity CreateSubTypeOfUseEntity(int id = 1)
    {
        var entity = new SubTypeOfUseEntity
        {
            Description = $"Sub Type of Use {id}",
            TypeOfUseId = 1,
            IsActive = true
        };
        var idProperty = typeof(SubTypeOfUseEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }

    public static SocietyDetailsEntity CreateSocietyDetailsEntity(int id = 1)
    {
        var entity = new SocietyDetailsEntity();
        var idProperty = typeof(SocietyDetailsEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static PropertyDescriptionAndTypeOfUseValidationEntity CreatePropertyDescriptionAndTypeOfUseValidationEntity(int id = 1)
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity();
        var idProperty = typeof(PropertyDescriptionAndTypeOfUseValidationEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }

    public static TaxPercentageMasterCVEntity CreateTaxPercentageMasterCVEntity(int id = 1)
    {
        var entity = new TaxPercentageMasterCVEntity();
        var idProperty = typeof(TaxPercentageMasterCVEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        entity.IsActive = true;
        return entity;
    }
}
