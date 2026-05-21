using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for PropertyEntity to achieve 100% code coverage
/// </summary>
public class PropertyEntityTests
{
    #region Constructor and Basic Properties Tests

    [Fact]
    public void PropertyEntity_CanBeInstantiated()
    {
        // Act
        var entity = new PropertyEntity();

        // Assert
        Assert.NotNull(entity);
    }

    [Fact]
    public void PropertyEntity_InheritsFromBaseEntity()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void PropertyEntity_ImplementsIHardDeletable()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Assert
        Assert.IsAssignableFrom<IHardDeletable>(entity);
    }

    #endregion

    #region PropertySeqNo Tests

    [Fact]
    public void PropertySeqNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertySeqNo = 123;

        // Assert
        Assert.Equal(123, entity.PropertySeqNo);
    }

    [Fact]
    public void PropertySeqNo_CanBeNull()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertySeqNo = null;

        // Assert
        Assert.Null(entity.PropertySeqNo);
    }

    #endregion

    #region Location Information Tests

    [Fact]
    public void MoujaId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.MoujaId = 456;

        // Assert
        Assert.Equal(456, entity.MoujaId);
    }

    [Fact]
    public void TaxZoneId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.TaxZoneId = 789;

        // Assert
        Assert.Equal(789, entity.TaxZoneId);
    }

    [Fact]
    public void WardId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.WardId = 101;

        // Assert
        Assert.Equal(101, entity.WardId);
    }

    [Fact]
    public void PropertyNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertyNo = "PROP-001";

        // Assert
        Assert.Equal("PROP-001", entity.PropertyNo);
    }

    [Fact]
    public void PartitionNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PartitionNo = "PART-001";

        // Assert
        Assert.Equal("PART-001", entity.PartitionNo);
    }

    #endregion

    #region Property Classification Tests

    [Fact]
    public void PropertyTypeId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertyTypeId = 5;

        // Assert
        Assert.Equal(5, entity.PropertyTypeId);
    }

    [Fact]
    public void UPICId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.UPICId = "UPIC-12345";

        // Assert
        Assert.Equal("UPIC-12345", entity.UPICId);
    }

    [Fact]
    public void OpenPlot_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OpenPlot = true;

        // Assert
        Assert.True(entity.OpenPlot);
    }

    [Fact]
    public void CSN_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.CSN = "CSN-001";

        // Assert
        Assert.Equal("CSN-001", entity.CSN);
    }

    [Fact]
    public void SubZoneNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.SubZoneNo = "SZ-001";

        // Assert
        Assert.Equal("SZ-001", entity.SubZoneNo);
    }

    [Fact]
    public void PlotNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PlotNo = "PLOT-001";

        // Assert
        Assert.Equal("PLOT-001", entity.PlotNo);
    }

    [Fact]
    public void CategoryId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.CategoryId = 10;

        // Assert
        Assert.Equal(10, entity.CategoryId);
    }

    [Fact]
    public void Type_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.Type = "Residential";

        // Assert
        Assert.Equal("Residential", entity.Type);
    }

   

    #endregion

    #region Owner Information Tests

    [Fact]
    public void OwnerTitle_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act - Using real Marathi characters
        entity.OwnerTitle = "????";

        // Assert
        Assert.Equal("????", entity.OwnerTitle);
    }

    [Fact]
    public void OwnerName_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act - Using real Marathi name
        entity.OwnerName = "??? ?????";

        // Assert
        Assert.Equal("??? ?????", entity.OwnerName);
    }

    [Fact]
    public void OwnerTitleEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OwnerTitleEnglish = "Mr.";

        // Assert
        Assert.Equal("Mr.", entity.OwnerTitleEnglish);
    }

    [Fact]
    public void OwnerNameEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OwnerNameEnglish = "Raj Kumar";

        // Assert
        Assert.Equal("Raj Kumar", entity.OwnerNameEnglish);
    }

    #endregion

    #region Occupier Information Tests

    [Fact]
    public void OccupierTitle_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierTitle = "???????";

        // Assert
        Assert.Equal("???????", entity.OccupierTitle);
    }

    [Fact]
    public void OccupierName_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierName = "???? ????";

        // Assert
        Assert.Equal("???? ????", entity.OccupierName);
    }

    [Fact]
    public void OccupierTitleEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierTitleEnglish = "Mrs.";

        // Assert
        Assert.Equal("Mrs.", entity.OccupierTitleEnglish);
    }

    [Fact]
    public void OccupierNameEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierNameEnglish = "Sita Devi";

        // Assert
        Assert.Equal("Sita Devi", entity.OccupierNameEnglish);
    }

    #endregion

    #region Flat/Shop Information Tests

    [Fact]
    public void FlatOrShopNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.FlatOrShopNo = "101";

        // Assert
        Assert.Equal("101", entity.FlatOrShopNo);
    }

    [Fact]
    public void FlatOrShopName_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.FlatOrShopName = "????? ???";

        // Assert
        Assert.Equal("????? ???", entity.FlatOrShopName);
    }

    [Fact]
    public void FlatOrShopNoEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.FlatOrShopNoEnglish = "101A";

        // Assert
        Assert.Equal("101A", entity.FlatOrShopNoEnglish);
    }

    [Fact]
    public void FlatOrShopNameEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.FlatOrShopNameEnglish = "Shop Name";

        // Assert
        Assert.Equal("Shop Name", entity.FlatOrShopNameEnglish);
    }

    #endregion

    #region Address Information Tests

    [Fact]
    public void Address_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.Address = "?????, ??????????";

        // Assert
        Assert.Equal("?????, ??????????", entity.Address);
    }

    [Fact]
    public void Location_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act - Using real Marathi location
        entity.Location = "????";

        // Assert
        Assert.Equal("????", entity.Location);
    }

    [Fact]
    public void AddressEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.AddressEnglish = "Mumbai, Maharashtra";

        // Assert
        Assert.Equal("Mumbai, Maharashtra", entity.AddressEnglish);
    }

    [Fact]
    public void LocationEnglish_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.LocationEnglish = "Dadar";

        // Assert
        Assert.Equal("Dadar", entity.LocationEnglish);
    }

    #endregion

    #region Contact Information Tests

    [Fact]
    public void MobileNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.MobileNo = "9876543210";

        // Assert
        Assert.Equal("9876543210", entity.MobileNo);
    }

    [Fact]
    public void EmailId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.EmailId = "test@example.com";

        // Assert
        Assert.Equal("test@example.com", entity.EmailId);
    }

    [Fact]
    public void PinCode_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PinCode = "400028";

        // Assert
        Assert.Equal("400028", entity.PinCode);
    }

    [Fact]
    public void MobileNoRemarkId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.MobileNoRemarkId = 5;

        // Assert
        Assert.Equal(5, entity.MobileNoRemarkId);
    }

    [Fact]
    public void AlternateMobileNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.AlternateMobileNo = "9876543211";

        // Assert
        Assert.Equal("9876543211", entity.AlternateMobileNo);
    }

    [Fact]
    public void OccupierMobileNo_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierMobileNo = "9876543212";

        // Assert
        Assert.Equal("9876543212", entity.OccupierMobileNo);
    }

    [Fact]
    public void OccupierMobileNoRemarkId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.OccupierMobileNoRemarkId = 7;

        // Assert
        Assert.Equal(7, entity.OccupierMobileNoRemarkId);
    }

    #endregion

    #region Society and Status Information Tests

    [Fact]
    public void SocietyDetailId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.SocietyDetailId = 15;

        // Assert
        Assert.Equal(15, entity.SocietyDetailId);
    }

    [Fact]
    public void PropertyAssessmentStatusId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertyAssessmentStatusId = 20;

        // Assert
        Assert.Equal(20, entity.PropertyAssessmentStatusId);
    }

    [Fact]
    public void PropertyMastOldId_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertyMastOldId = 25;

        // Assert
        Assert.Equal(25, entity.PropertyMastOldId);
    }

    #endregion

    #region IHardDeletable Tests

    [Fact]
    public void MarkedForDeletion_DefaultsToFalse()
    {
        // Arrange & Act
        var entity = new PropertyEntity();

        // Assert
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void MarkedForDeletion_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.MarkedForDeletion = true;

        // Assert
        Assert.True(entity.MarkedForDeletion);
    }

    [Fact]
    public void MarkedForDeletionDate_CanBeSetAndGet()
    {
        // Arrange
        var entity = new PropertyEntity();
        var date = DateTime.Now;

        // Act
        entity.MarkedForDeletionDate = date;

        // Assert
        Assert.Equal(date, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void MarkedForDeletionDate_CanBeNull()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.MarkedForDeletionDate = null;

        // Assert
        Assert.Null(entity.MarkedForDeletionDate);
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void PolicyTaxDetails_InitializesAsEmptyCollection()
    {
        // Arrange & Act
        var entity = new PropertyEntity();

        // Assert
        Assert.NotNull(entity.PolicyTaxDetails);
        Assert.Empty(entity.PolicyTaxDetails);
    }

    [Fact]
    public void PolicyTaxDetails_CanAddItems()
    {
        // Arrange
        var entity = new PropertyEntity();
        var policyTaxDetail = new PolicyTaxDetailsEntity();

        // Act
        entity.PolicyTaxDetails.Add(policyTaxDetail);

        // Assert
        Assert.Single(entity.PolicyTaxDetails);
        Assert.Contains(policyTaxDetail, entity.PolicyTaxDetails);
    }

    #endregion

    #region Complete Property Coverage Tests

    [Fact]
    public void PropertyEntity_AllPropertiesCanBeSetToNull()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertySeqNo = null;
        entity.MoujaId = null;
        entity.PropertyNo = null;
        entity.PartitionNo = null;
        entity.PropertyTypeId = null;
        entity.UPICId = null;
        entity.OpenPlot = null;
        entity.CSN = null;
        entity.SubZoneNo = null;
        entity.PlotNo = null;
        entity.CategoryId = null;
        entity.Type = null;
        entity.OwnerTitle = null;
        entity.OwnerName = null;
        entity.OwnerTitleEnglish = null;
        entity.OwnerNameEnglish = null;
        entity.OccupierTitle = null;
        entity.OccupierName = null;
        entity.OccupierTitleEnglish = null;
        entity.OccupierNameEnglish = null;
        entity.FlatOrShopNo = null;
        entity.FlatOrShopName = null;
        entity.FlatOrShopNoEnglish = null;
        entity.FlatOrShopNameEnglish = null;
        entity.Address = null;
        entity.Location = null;
        entity.AddressEnglish = null;
        entity.LocationEnglish = null;
        entity.MobileNo = null;
        entity.EmailId = null;
        entity.PinCode = null;
        entity.MobileNoRemarkId = null;
        entity.AlternateMobileNo = null;
        entity.OccupierMobileNo = null;
        entity.OccupierMobileNoRemarkId = null;
        entity.SocietyDetailId = null;
        entity.PropertyAssessmentStatusId = null;
        entity.PropertyMastOldId = null;
        entity.MarkedForDeletionDate = null;

        // Assert - All nullable properties should be null
        Assert.Null(entity.PropertySeqNo);
        Assert.Null(entity.MoujaId);
        Assert.Null(entity.PropertyNo);
        Assert.Null(entity.PartitionNo);
        Assert.Null(entity.PropertyTypeId);
        Assert.Null(entity.UPICId);
        Assert.Null(entity.OpenPlot);
        Assert.Null(entity.CSN);
        Assert.Null(entity.SubZoneNo);
        Assert.Null(entity.PlotNo);
        Assert.Null(entity.CategoryId);
        Assert.Null(entity.Type);
        Assert.Null(entity.OwnerTitle);
        Assert.Null(entity.OwnerName);
        Assert.Null(entity.OwnerTitleEnglish);
        Assert.Null(entity.OwnerNameEnglish);
        Assert.Null(entity.OccupierTitle);
        Assert.Null(entity.OccupierName);
        Assert.Null(entity.OccupierTitleEnglish);
        Assert.Null(entity.OccupierNameEnglish);
        Assert.Null(entity.FlatOrShopNo);
        Assert.Null(entity.FlatOrShopName);
        Assert.Null(entity.FlatOrShopNoEnglish);
        Assert.Null(entity.FlatOrShopNameEnglish);
        Assert.Null(entity.Address);
        Assert.Null(entity.Location);
        Assert.Null(entity.AddressEnglish);
        Assert.Null(entity.LocationEnglish);
        Assert.Null(entity.MobileNo);
        Assert.Null(entity.EmailId);
        Assert.Null(entity.PinCode);
        Assert.Null(entity.MobileNoRemarkId);
        Assert.Null(entity.AlternateMobileNo);
        Assert.Null(entity.OccupierMobileNo);
        Assert.Null(entity.OccupierMobileNoRemarkId);
        Assert.Null(entity.SocietyDetailId);
        Assert.Null(entity.PropertyAssessmentStatusId);
        Assert.Null(entity.PropertyMastOldId);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyEntity_CompletePropertyInitialization()
    {
        // Arrange & Act
        var entity = new PropertyEntity
        {
            PropertySeqNo = 1,
            MoujaId = 2,
            TaxZoneId = 3,
            WardId = 4,
            PropertyNo = "PROP-001",
            PartitionNo = "PART-001",
            PropertyTypeId = 5,
            UPICId = "UPIC-001",
            OpenPlot = true,
            CSN = "CSN-001",
            SubZoneNo = "SZ-001",
            PlotNo = "PLOT-001",
            CategoryId = 6,
            Type = "Residential",    
            OwnerTitle = "Mr.",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr.",
            OwnerNameEnglish = "John Doe",
            OccupierTitle = "Mrs.",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Mrs.",
            OccupierNameEnglish = "Jane Doe",
            FlatOrShopNo = "101",
            FlatOrShopName = "Shop Name",
            FlatOrShopNoEnglish = "101",
            FlatOrShopNameEnglish = "Shop Name",
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main St",
            LocationEnglish = "Downtown",
            MobileNo = "1234567890",
            EmailId = "test@example.com",
            PinCode = "400001",
            MobileNoRemarkId = 7,
            AlternateMobileNo = "0987654321",
            OccupierMobileNo = "1112223333",
            OccupierMobileNoRemarkId = 8,
            SocietyDetailId = 9,
            PropertyAssessmentStatusId = 10,
            PropertyMastOldId = 11,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, entity.PropertySeqNo);
        Assert.Equal(2, entity.MoujaId);
        Assert.Equal(3, entity.TaxZoneId);
        Assert.Equal(4, entity.WardId);
        Assert.Equal("PROP-001", entity.PropertyNo);
        Assert.Equal("PART-001", entity.PartitionNo);
        Assert.Equal(5, entity.PropertyTypeId);
        Assert.Equal("UPIC-001", entity.UPICId);
        Assert.True(entity.OpenPlot);
        Assert.Equal("CSN-001", entity.CSN);
        Assert.Equal("SZ-001", entity.SubZoneNo);
        Assert.Equal("PLOT-001", entity.PlotNo);
        Assert.Equal(6, entity.CategoryId);
        Assert.Equal("Residential", entity.Type);
        Assert.Equal("Mr.", entity.OwnerTitle);
        Assert.Equal("John Doe", entity.OwnerName);
        Assert.Equal("Mr.", entity.OwnerTitleEnglish);
        Assert.Equal("John Doe", entity.OwnerNameEnglish);
        Assert.Equal("Mrs.", entity.OccupierTitle);
        Assert.Equal("Jane Doe", entity.OccupierName);
        Assert.Equal("Mrs.", entity.OccupierTitleEnglish);
        Assert.Equal("Jane Doe", entity.OccupierNameEnglish);
        Assert.Equal("101", entity.FlatOrShopNo);
        Assert.Equal("Shop Name", entity.FlatOrShopName);
        Assert.Equal("101", entity.FlatOrShopNoEnglish);
        Assert.Equal("Shop Name", entity.FlatOrShopNameEnglish);
        Assert.Equal("123 Main St", entity.Address);
        Assert.Equal("Downtown", entity.Location);
        Assert.Equal("123 Main St", entity.AddressEnglish);
        Assert.Equal("Downtown", entity.LocationEnglish);
        Assert.Equal("1234567890", entity.MobileNo);
        Assert.Equal("test@example.com", entity.EmailId);
        Assert.Equal("400001", entity.PinCode);
        Assert.Equal(7, entity.MobileNoRemarkId);
        Assert.Equal("0987654321", entity.AlternateMobileNo);
        Assert.Equal("1112223333", entity.OccupierMobileNo);
        Assert.Equal(8, entity.OccupierMobileNoRemarkId);
        Assert.Equal(9, entity.SocietyDetailId);
        Assert.Equal(10, entity.PropertyAssessmentStatusId);
        Assert.Equal(11, entity.PropertyMastOldId);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void MarkedForDeletion_DefaultsToFalse_UponInstantiation()
    {
        // Act - Create new instance without setting MarkedForDeletion
        var entity = new PropertyEntity();

        // Assert - Verify default value is false (covers line 110)
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void MarkedForDeletion_ExplicitlySetToFalse_RemainsUnchanged()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act - Explicitly set to false
        entity.MarkedForDeletion = false;

        // Assert
        Assert.False(entity.MarkedForDeletion);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void PropertyEntity_DefaultConstructor_InitializesCollections()
    {
        // Act
        var entity = new PropertyEntity();

        // Assert
        Assert.NotNull(entity.PolicyTaxDetails);
        Assert.Empty(entity.PolicyTaxDetails);
    }

    [Fact]
    public void PropertyEntity_SetAllPropertiesToEmptyString()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act
        entity.PropertyNo = "";
        entity.PartitionNo = "";
        entity.UPICId = "";
        entity.CSN = "";
        entity.SubZoneNo = "";
        entity.PlotNo = "";
        entity.Type = "";
        entity.OwnerTitle = "";
        entity.OwnerName = "";
        entity.OwnerTitleEnglish = "";
        entity.OwnerNameEnglish = "";
        entity.OccupierTitle = "";
        entity.OccupierName = "";
        entity.OccupierTitleEnglish = "";
        entity.OccupierNameEnglish = "";
        entity.FlatOrShopNo = "";
        entity.FlatOrShopName = "";
        entity.FlatOrShopNoEnglish = "";
        entity.FlatOrShopNameEnglish = "";
        entity.Address = "";
        entity.Location = "";
        entity.AddressEnglish = "";
        entity.LocationEnglish = "";
        entity.MobileNo = "";
        entity.EmailId = "";
        entity.PinCode = "";
        entity.AlternateMobileNo = "";
        entity.OccupierMobileNo = "";

        // Assert
        Assert.Equal("", entity.PropertyNo);
        Assert.Equal("", entity.PartitionNo);
        Assert.Equal("", entity.UPICId);
        Assert.Equal("", entity.CSN);
        Assert.Equal("", entity.SubZoneNo);
        Assert.Equal("", entity.PlotNo);
        Assert.Equal("", entity.Type);
        Assert.Equal("", entity.OwnerTitle);
        Assert.Equal("", entity.OwnerName);
        Assert.Equal("", entity.OwnerTitleEnglish);
        Assert.Equal("", entity.OwnerNameEnglish);
        Assert.Equal("", entity.OccupierTitle);
        Assert.Equal("", entity.OccupierName);
        Assert.Equal("", entity.OccupierTitleEnglish);
        Assert.Equal("", entity.OccupierNameEnglish);
        Assert.Equal("", entity.FlatOrShopNo);
        Assert.Equal("", entity.FlatOrShopName);
        Assert.Equal("", entity.FlatOrShopNoEnglish);
        Assert.Equal("", entity.FlatOrShopNameEnglish);
        Assert.Equal("", entity.Address);
        Assert.Equal("", entity.Location);
        Assert.Equal("", entity.AddressEnglish);
        Assert.Equal("", entity.LocationEnglish);
        Assert.Equal("", entity.MobileNo);
        Assert.Equal("", entity.EmailId);
        Assert.Equal("", entity.PinCode);
        Assert.Equal("", entity.AlternateMobileNo);
        Assert.Equal("", entity.OccupierMobileNo);
    }

    [Fact]
    public void OpenPlot_CanBeSetAndGet_AllCombinations()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act & Assert - null
        entity.OpenPlot = null;
        Assert.Null(entity.OpenPlot);

        // Act & Assert - true
        entity.OpenPlot = true;
        Assert.True(entity.OpenPlot);

        // Act & Assert - false
        entity.OpenPlot = false;
        Assert.False(entity.OpenPlot);
    }

    [Fact]
    public void MarkedForDeletion_ToggleBetweenTrueAndFalse()
    {
        // Arrange
        var entity = new PropertyEntity();

        // Act & Assert - initial false
        Assert.False(entity.MarkedForDeletion);

        // Act & Assert - set to true
        entity.MarkedForDeletion = true;
        Assert.True(entity.MarkedForDeletion);

        // Act & Assert - set back to false
        entity.MarkedForDeletion = false;
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void PolicyTaxDetails_CanRemoveItems()
    {
        // Arrange
        var entity = new PropertyEntity();
        var policyTaxDetail = new PolicyTaxDetailsEntity();
        entity.PolicyTaxDetails.Add(policyTaxDetail);

        // Act
        entity.PolicyTaxDetails.Remove(policyTaxDetail);

        // Assert
        Assert.Empty(entity.PolicyTaxDetails);
    }

    #endregion
}
