using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Infrastructure.Data;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<ConstructionTypeEntity> ConstructionTypeEntity { get; set; } = null!;
    public DbSet<FloorEntity> FloorEntity { get; set; } = null!;
    public DbSet<SubFloorEntity> SubFloorEntity { get; set; } = null!;
    public DbSet<RateEntity> RateEntity { get; set; } = null!;
    public DbSet<MultilingualResourceEntity> MultilingualResourceEntity { get; set; } = null!;
    public DbSet<RateMasterForCVEntity> RateMasterForCVs { get; set; } = null!;
    public DbSet<CSNDetailsEntity> CSNDetails { get; set; } = null!;
    public DbSet<TaxZoneEntity> TaxZoneMaster { get; set; } = null!;
    public DbSet<AssessmentYearRangeEntity> AssessmentYearRangeEntities { get; set; } = null!;
    public DbSet<RetentionFactWiseEntity> RetentionFactWiseEntities { get; set; } = null!;
    public DbSet<UserRoleMasterEntity> UserRoleMasterEntity { get; set; } = null!;
    public DbSet<MoujaEntity> MoujaEntity { get; set; } = null!;
    public DbSet<CombinePropertyHistoryEntity> CombinePropertyHistory { get; set; } = null!;
    public DbSet<PropertyScreenLockEntity> PropertyScreenLocks { get; set; } = null!;
    public DbSet<OfficeEntity> OfficeEntity { get; set; } = null!;
    public DbSet<RetentionYearWiseEntity> RetentionYearWiseEntities { get; set; } = null!;
    public DbSet<SubTypeOfUseEntity> SubTypeOfUse { get; set; } = null!;
    public DbSet<TypeOfUseEntity> TypeOfUse { get; set; } = null!;
    public DbSet<PolicyConfigurationEntity> PolicyConfiguration { get; set; } = null!;
    public DbSet<AssessmentYearRangeCVEntity> AssessmentYearRangeCVEntities { get; set; } = null!;
    public DbSet<TypeOfUseGroupEntity> TypeOfUseGroup { get; set; } = null!;
    public DbSet<DepreciationMasterEntity> DepreciationMaster { get; set; } = null!;
    public DbSet<ZoneEntity> ZoneMaster { get; set; } = null!;
    public DbSet<WardEntity> WardMaster { get; set; } = null!;
    public DbSet<BankMasterEntity> BankMasters { get; set; } = null!;
    public DbSet<PropertyRuleEvaluationMasterEntity> PropertyRuleEvaluationMaster { get; set; } = null!;
    public DbSet<YearMasterEntity> YearMaster { get; set; } = null!;
    public DbSet<ScreenMasterEntity> ScreenMaster { get; set; } = null!;
    public DbSet<ScreenGroupMasterEntity> ScreenGroupMaster { get; set; } = null!;
    public DbSet<RoleWiseScreenAccessMasterEntity> RoleWiseScreenAccessMasters { get; set; } = null!;
    public DbSet<RateSectionEntity> RateSection { get; set; } = null!;
    public DbSet<ModuleMasterEntity> ModuleMasters { get; set; } = null!;
    public DbSet<ActiveTaxesEntity> ActiveTaxesMasters { get; set; } = null!;
    public DbSet<DepartmentLicenceDetailsEntity> DepartmentLicenceDetails { get; set; } = null!;
    public DbSet<RateSectionDetailsEntity> RateSectionDetails { get; set; } = null!;
    public DbSet<ScreenGroupMasterEntity> ScreenGroupMasters { get; set; } = null!;
    public DbSet<YearMasterEntity> YearMasterEntity { get; set; } = null!;
    public DbSet<DesignationMasterEntity> DesignationMasters { get; set; } = null!;
    public DbSet<DepartmentMasterEntity> DepartmentMasters { get; set; } = null!;
    public DbSet<GrievanceCategoryEntity> GrievanceCategory { get; set; } = null!;
    public DbSet<PropertyEntity> PropertyMast { get; set; } = null!;
    public DbSet<ULBMasterEntity> ULBMasters { get; set; } = null!;
    public DbSet<PropertyCategoryEntity> PropertyCategoryMaster { get; set; } = null!;
    public DbSet<PropertyAssessmentEntity> PropertyMastDetails { get; set; } = null!;
    public DbSet<PropertyDetailsEntity> PropertyDetails { get; set; } = null!;
    public DbSet<PlotDetailsEntity> PlotDetails { get; set; } = null!;
    public DbSet<ConfigCategoryMasterEntity> ConfigCategoryMasters { get; set; } = null!;
    public DbSet<ConfigKeyMasterEntity> ConfigKeyMasters { get; set; } = null!;
    public DbSet<PropertyDetailsReassessmentEntity> PropertyDetailsReassessment { get; set; } = null!;
    public DbSet<PaymentModeEntity> PaymentModeEntity { get; set; } = null!;
    public DbSet<WingEntity> WingEntity { get; set; } = null!;
    public DbSet<SocietyDetailsEntity> SocietyDetailsMast { get; set; } = null!;
    public DbSet<OwnerTypeMasterEntity> OwnerTypeMaster { get; set; } = null!;
    public DbSet<PropertyMastOldEntity> PropertyMastOld { get; set; } = null!;
    public DbSet<PropertyDetailsOldEntity> PropertyDetailsOld { get; set; } = null!;
    public DbSet<ConfigValueMasterEntity> ConfigValueMasters { get; set; } = null!;
    public DbSet<PropertyTypeCategoryEntity> PropertyTypeCategoryMaster { get; set; } = null!;
    public DbSet<PropertyTypeMasterEntity> PropertyTypeMasters { get; set; } = null!;
    public DbSet<PropertyPhotoTypeEntity> PropertyPhotoTypes { get; set; } = null!;
    public DbSet<PropertySocialDetailsEntity> PropertySocialDetails { get; set; } = null!;
    public DbSet<TransMastCVEntity> TransMastCV { get; set; } = null!;
    public DbSet<TransMastRVEntity> TransMastRV { get; set; } = null!;
    public DbSet<UserEntity> UserMasters { get; set; } = null!;
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;
    public DbSet<PropertyTaxCalculationCVResultsEntity> PropertyTaxCalculationCVResults { get; set; } = null!;
    public DbSet<PropertyTaxCalculationRVResultsEntity> PropertyTaxCalculationRVResults { get; set; } = null!;
    public DbSet<NatureFactorCVMasterEntity> NatureFactorCVMasters { get; set; } = null!;
    public DbSet<AgeFactorCVMasterEntity> AgeFactorCVMasters { get; set; } = null!;
    public DbSet<FloorFactorCVMasterEntity> FloorFactorCVMasters { get; set; } = null!;
    public DbSet<TaxPercentageMasterCVEntity> TaxPercentageMasterCVs { get; set; } = null!;
    public DbSet<TaxMasterEntity> TaxMaster { get; set; } = null!;
    public DbSet<TaxCategoryMasterEntity> TaxCategoryMaster { get; set; } = null!;
    public DbSet<FlagMasterEntity> FlagMaster { get; set; } = null!;
    public DbSet<TransMastOldEntity> TransMastOld { get; set; } = null!;

    public DbSet<RenterDetailEntity> RenterDetails { get; set; } = null!;
    public DbSet<RoomWiseSubmissionDetailsEntity> RoomWiseSubmissionDetails { get; set; } = null!;
    public DbSet<UserDepartmentAllocationEntity> UserDepartmentAllocation { get; set; } = null!;
    public DbSet<UserModuleAllocationEntity> UserModuleAllocation { get; set; } = null!;
    public DbSet<UserRoleAllocationEntity> UserRoleAllocation { get; set; } = null!;
    public DbSet<EmployeeTypeEntity> EmployeeType { get; set; } = null!;
    public DbSet<PropertyDescriptionAndTypeOfUseValidationEntity> PropertyDescriptionAndTypeOfUseValidations { get; set; } = null!;
    public DbSet<GenderMasterEntity> GenderMasters { get; set; } = null!;
    public DbSet<PropertyCertificateTypeMasterEntity> PropertyCertificateTypeMasters { get; set; } = null!;
    public DbSet<FloorGroupMasterEntity> FloorGroupMaster { get; set; } = null!;
    public DbSet<PolicyTaxDetailsCVEntity> PolicyTaxDetailsCV { get; set; } = null!;

    public DbSet<BlockMasterEntity> BlockMasters { get; set; } = null!;
    public DbSet<PropertyCertificateEntity> PropertyCertificates { get; set; } = null!;
    public DbSet<PropertyPhotoEntity> PropertyPhotos { get; set; } = null!;
    public DbSet<DocumentEntity> Documents { get; set; } = null!;
    public DbSet<DocumentBindingEntity> DocumentBindings { get; set; } = null!;
    public DbSet<TaxPercentageMasterRVEntity> TaxPercentageMasterRVs { get; set; } = null!;

    public DbSet<UseFactorCVMasterEntity> UseFactorCVMaster { get; set; } = null!;
    public DbSet<ParkingTypeMasterEntity> ParkingTypeMaster { get; set; } = null!;
    public DbSet<RuleScopeEntity> RuleScope { get; set; } = null!;
    public DbSet<PolicyTaxDetailsEntity> PolicyTaxDetails { get; set; } = null!;
    public DbSet<CommonRemarkTypeMasterEntity> CommonRemarkTypeMasters { get; set; } = null!;
    public DbSet<RuleEffectTypeEntity> RuleEffectTypeMaster { get; set; } = null!;

    public DbSet<RenterMastEntity> RenterMast { get; set; } = null!;
    public DbSet<CommonRemarkDetailsEntity> CommonRemarkDetails { get; set; } = null!;
    public DbSet<PropertyMapMasterEntity> PropertyMapMasters { get; set; } = null!;
    public DbSet<RoomTypeMasterEntity> RoomTypeMasters { get; set; } = null!;

    public DbSet<WaterConnectionTypeEntity> WaterConnectionTypes { get; set; } = null!;
    public DbSet<WaterConnectionSizeEntity> WaterConnectionSizes { get; set; } = null!;
    public DbSet<WaterConnectionStatusEntity> WaterConnectionStatuses { get; set; } = null!;
    public DbSet<WaterRateMasterEntity> WaterRateMasters { get; set; } = null!;
    public DbSet<WaterConnectionMasterEntity> WaterConnectionMasters { get; set; } = null!;
    public DbSet<WaterConnectionDetailsEntity> WaterConnectionDetails { get; set; } = null!;
    public DbSet<BulkUpdateMasterEntity> BulkUpdateMasters { get; set; } = null!;
    public DbSet<BulkUpdateFieldConfigEntity> BulkUpdateFieldConfigs { get; set; } = null!;
    public DbSet<BulkUpdateHistoryEntity> BulkUpdateHistory { get; set; } = null!;
    //Asset Start
    public DbSet<InventoryItemCategoryEntity> InventoryItemCategory { get; set; } = null!;
    public DbSet<InventoryItemNameEntity> InventoryItemName { get; set; } = null!;
    public DbSet<InventoryItemConditionEntity> InventoryItemCondition { get; set; } = null!;
    public DbSet<InventoryItemModelEntity> InventoryItemModelMaster { get; set; } = null!;
    public DbSet<EducationTaxMasterEntity> EducationTaxMasters { get; set; } = null!;
    public DbSet<ScreenEntity> AssetScreen { get; set; } = null!;
    public DbSet<ScreenFormSectionMasterEntity> ScreenFormSectionMaster { get; set; } = null!;
    public DbSet<ScreenFormFieldMasterEntity> ScreenFormFieldMaster { get; set; } = null!;
    public DbSet<SocialAttributeEntity> SocialAttribute { get; set; } = null!;
    public DbSet<TypeOfUseGroupCVEntity> TypeOfUseGroupMasterCV { get; set; } = null!;

    public DbSet<EmploymentTaxMasterEntity> EmploymentTaxMasters { get; set; } = null!;
    public DbSet<AssetTypeEntity> AssetType { get; set; } = null!;
    public DbSet<AssetCategoryEntity> AssetCategory { get; set; } = null!;
    public DbSet<OwnershipTypeEntity> OwnershipType { get; set; } = null!;
    public DbSet<OwningDepartmentEntity> OwningDepartment { get; set; } = null!;
    public DbSet<RulesFieldEntity> RulesField { get; set; } = null!;
    public DbSet<RuleScopeFieldMappingEntity> RuleScopeFieldMapping { get; set; } = null!;
    public DbSet<FieldConfigurationEntity> FieldConfiguration { get; set; } = null!;
    public DbSet<EffectTypeConfigurationEntity> EffectTypeConfiguration { get; set; } = null!;
    public DbSet<RuleEngineEntity> RuleEngine { get; set; } = null!;
    public DbSet<RuleVersionHistoryEntity> RuleVersionHistory { get; set; } = null!;
    public DbSet<RuleCategoryEntity> RuleCategory { get; set; } = null!;

    // New child table entities with FK to PropertyMast
    public DbSet<ApplyTaxesMasterEntity> ApplyTaxesMaster { get; set; } = null!;
    public DbSet<PropertyAssessmentDetailsEntity> PropertyAssessmentDetails { get; set; } = null!;
    public DbSet<PropertyTaxCalculationSection129ResultsEntity> PropertyTaxCalculationSection129Results { get; set; } = null!;
    public DbSet<PropertyOccupancyDetailsEntity> PropertyOccupancyDetails { get; set; } = null!;
    public DbSet<PropertyAssessmentStatusEntity> PropertyAssessmentStatuses { get; set; } = null!;
    public DbSet<PropertyImagesMastEntity> PropertyImagesMast { get; set; } = null!;
    public DbSet<TaxPendingDetailsArchiveEntity> TaxPendingDetailsArchive { get; set; } = null!;
    public DbSet<TaxPendingDetailsCVEntity> TaxPendingDetailsCV { get; set; } = null!;
    public DbSet<TaxPendingDetailsLookupEntity> TaxPendingDetailsLookup { get; set; } = null!;
    public DbSet<TaxPendingDetailsRetroEntity> TaxPendingDetailsRetro { get; set; } = null!;
    public DbSet<TaxPendingDetailsRVEntity> TaxPendingDetailsRV { get; set; } = null!;
    public DbSet<TaxPendingDetailsEntity> TaxPendingDetails { get; set; } = null!;
    public DbSet<TransMastEntity> TransMast { get; set; } = null!;
    public DbSet<TransMastArchiveEntity> TransMastArchive { get; set; } = null!;
    public DbSet<TransMastLookupEntity> TransMastLookup { get; set; } = null!;
    public DbSet<RoomWiseMinusDataEntity> RoomWiseMinusData { get; set; } = null!;
    public DbSet<AssetDocumentDefinitionEntity> AssetDocumentDefinitions { get; set; } = null!;
    public DbSet<AssetFieldDefinitionEntity> AssetFieldDefinitions { get; set; } = null!;
    public DbSet<AssetAuthorityMasterEntity> AssetAuthorityMasters { get; set; } = null!;
    public DbSet<AssetOrganizationMasterEntity> AssetOrganizationMasters { get; set; } = null!;
    public DbSet<SubZoneDetailsForCVEntity> SubZoneDetailsForCV { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<PolicyTaxDetailsEntity>(entity =>
        {
            entity.ToTable("PolicyTaxDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PolicyCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PolicyDate).HasColumnType("datetime");
            entity.Property(e => e.PolicyYear);
            entity.Property(e => e.PolicyReason).HasMaxLength(200);
            entity.Property(e => e.PolicyRVorCVvalue).HasColumnType("money");
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.TaxAmount).HasColumnType("money");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            // Configure foreign key relationships
            entity.HasOne(e => e.TaxMaster)
                .WithMany(p => p.PolicyTaxDetails)
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PolicyTaxDetails)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for better query performance
            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.TaxId);
        });

        // NatureFactorCVMaster configuration
        modelBuilder.Entity<NatureFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("NatureFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.ConstructionTypeId).IsRequired();
            entity.Property(e => e.Factor)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.ConstructionTypeId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.IsActive);
            // Uniqueness constraint for ConstructionTypeId + YearRangeCVId
            entity.HasIndex(e => new { e.ConstructionTypeId, e.YearRangeCVId }).IsUnique();

            entity.HasOne(e => e.ConstructionType)
             .WithMany(c => c.NatureFactorCVMaster)
             .HasForeignKey(e => e.ConstructionTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.YearRangeCV)
           .WithMany(c => c.NatureFactorCVMaster)
           .HasForeignKey(e => e.YearRangeCVId)
           .OnDelete(DeleteBehavior.Restrict);
        });


        // ParkingTypeMaster configuration
        modelBuilder.Entity<ParkingTypeMasterEntity>(entity =>
        {
            entity.ToTable("ParkingTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.TypeOfUseId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.TypeOfUseId);

            entity.HasOne(e => e.TypeOfUse)
            .WithMany(c => c.ParkingTypeMaster)
            .HasForeignKey(e => e.TypeOfUseId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConstructionTypeEntity>(entity =>
        {
            entity.ToTable("ConstructionTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConstructionCode);
            entity.Property(e => e.Description);
            entity.Property(e => e.SearchSequence);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Configure relationships
            entity.HasMany(e => e.Rates)
                .WithOne(r => r.ConstructionType)
                .HasForeignKey(r => r.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.NatureFactorCVMaster)
                .WithOne(n => n.ConstructionType)
                .HasForeignKey(n => n.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AgeFactorCVMaster)
                .WithOne(a => a.ConstructionType)
                .HasForeignKey(a => a.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertyDetails)
                .WithOne(p => p.ConstructionType)
                .HasForeignKey(p => p.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.DepreciationMaster)
                .WithOne(d => d.ConstructionType)
                .HasForeignKey(d => d.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FloorEntity>(entity =>
        {
            entity.ToTable("FloorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FloorCode);
            entity.Property(e => e.Description);
            entity.Property(e => e.SequenceNo);
            entity.Property(e => e.MaxFloorNo);
            entity.Property(e => e.FloorGroupId).HasColumnName("FloorGroupId");

            // Configure relationships
            entity.HasMany(e => e.Rates)
                .WithOne(r => r.Floor)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.FloorFactorCVMaster)
                .WithOne(n => n.Floor)
                .HasForeignKey(n => n.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertyDetails)
                .WithOne(a => a.Floor)
                .HasForeignKey(a => a.FloorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RateEntity>(entity =>
        {
            entity.ToTable("RateMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id);
            entity.Property(e => e.FloorId);
            entity.Property(e => e.Id);
            entity.Property(e => e.ConstructionTypeId);
            entity.Property(e => e.YearRangeRVId);
            entity.Property(e => e.RateSquareMeter).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RateSquareFeet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Id);
            entity.Property(e => e.RateRemark);
            entity.Property(e => e.IsActive);

            entity.HasOne(e => e.ConstructionType)
              .WithMany(c => c.Rates)
              .HasForeignKey(e => e.ConstructionTypeId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Floor)
              .WithMany(c => c.Rates)
              .HasForeignKey(e => e.FloorId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RateSection)
              .WithMany(c => c.Rates)
              .HasForeignKey(e => e.RateSectionId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TaxZone)
              .WithMany(c => c.Rates)
              .HasForeignKey(e => e.TaxZoneId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TypeOfUseGroup)
              .WithMany(c => c.Rates)
              .HasForeignKey(e => e.TypeOfUseGroupId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssessmentYearRange)
            .WithMany(c => c.Rates)
            .HasForeignKey(e => e.YearRangeRVId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RetentionFactWiseEntity>(entity =>
        {
            entity.ToTable("RetentionPolicyFactorWiseMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromFactor);
            entity.Property(e => e.ToFactor);
            entity.Property(e => e.FactorValue);
            entity.Property(e => e.IsActive);
        });

        modelBuilder.Entity<PropertyScreenLockEntity>(entity =>
        {
            entity.ToTable("PropertyScreenLock", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.LockableScreenId).IsRequired();
            entity.Property(e => e.IsLocked).IsRequired();  // Removed HasDefaultValue to ensure value is always sent
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.LockedDate).HasColumnType("datetime");
            entity.Property(e => e.UnlockedDate).HasColumnType("datetime");
            entity.Property(e => e.LockedBy);  // Explicitly configure
            entity.Property(e => e.UnlockedBy);  // Explicitly configure

            entity.HasIndex(e => new { e.PropertyId, e.LockableScreenId })
                .IsUnique()
                .HasDatabaseName("UQ_PropertyScreenLock_Property_Screen");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_PropertyScreenLock_PropertyId");
            entity.HasIndex(e => e.LockableScreenId).HasDatabaseName("IX_PropertyScreenLock_LockableScreenId");

            entity.HasOne(e => e.Property)
                .WithMany()
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LockableScreen)
                .WithMany()
                .HasForeignKey(e => e.LockableScreenId)
                .HasConstraintName("FK_PropertyScreenLock_ScreenMaster")
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<RetentionYearWiseEntity>(entity =>
        {
            entity.ToTable("RetentionPolicyYearWiseMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromYear);
            entity.Property(e => e.ToYear);
            entity.Property(e => e.FactorValue);
            entity.Property(e => e.IsActive);
        });
        modelBuilder.Entity<AssessmentYearRangeEntity>(entity =>
        {
            entity.ToTable("AssessmentYearRangeMasterRV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromYear);
            entity.Property(e => e.ToYear);
            entity.Property(e => e.IsActive);
            // Unique constraint for FromYear-ToYear pair
            entity.HasIndex(e => new { e.FromYear, e.ToYear }).IsUnique();

            // Configure relationships
            entity.HasMany(e => e.Rates)
                .WithOne(r => r.AssessmentYearRange)
                .HasForeignKey(r => r.YearRangeRVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.DepreciationMaster)
            .WithOne(r => r.AssessmentYearRange)
            .HasForeignKey(r => r.YearRangeRVId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TaxPercentageMasterRV)
             .WithOne(r => r.AssessmentYearRange)
              .HasForeignKey(r => r.YearRangeRVId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AssessmentYearRangeCVEntity>(entity =>
        {
            entity.ToTable("AssessmentYearRangeMasterCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromYear);
            entity.Property(e => e.ToYear);
            entity.Property(e => e.IsActive);
            entity.HasIndex(e => new { e.FromYear, e.ToYear }).IsUnique();

            // Configure relationships
            entity.HasMany(e => e.FloorFactorCVMaster)
                .WithOne(r => r.YearRangeCV)
                .HasForeignKey(r => r.YearRangeCVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.UseFactorCVMaster)
            .WithOne(r => r.YearRangeCV)
            .HasForeignKey(r => r.YearRangeCVId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AgeFactorCVMaster)
             .WithOne(r => r.YearRangeCV)
               .HasForeignKey(r => r.YearRangeCVId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.NatureFactorCVMaster)
                .WithOne(r => r.YearRangeCV)
                .HasForeignKey(r => r.YearRangeCVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RateMasterForCV)
              .WithOne(r => r.AssessmentYearRange)
               .HasForeignKey(r => r.AssessmentYearRangeId)
               .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TaxPercentageMasterCV)
             .WithOne(r => r.AssessmentYearRangeCV)
             .HasForeignKey(r => r.YearRangeCVId)
              .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SubFloorEntity>(entity =>
        {
            entity.ToTable("SubFloorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubFloorCode);
            entity.Property(e => e.Description);
            entity.Property(e => e.SubFloorPercentage);
            // Configure relationships
            entity.HasMany(e => e.PropertyDetails)
                .WithOne(r => r.SubFloor)
                .HasForeignKey(r => r.SubFloorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WardEntity>(entity =>
        {
            entity.ToTable("WardMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.WardNo).IsRequired().HasMaxLength(10);
            entity.Property(x => x.ZoneId).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(50);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(x => x.WardNo).IsUnique();

            entity.HasOne(e => e.Zone)
              .WithMany(c => c.Ward)
              .HasForeignKey(e => e.ZoneId)
              .OnDelete(DeleteBehavior.Restrict);

            // Configure relationships
            entity.HasMany(e => e.BlockMaster)
                .WithOne(r => r.Ward)
                .HasForeignKey(r => r.WardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RateSectionDetails)
                .WithOne(n => n.Ward)
                .HasForeignKey(n => n.WardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Property)
                .WithOne(a => a.Ward)
                .HasForeignKey(a => a.WardId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubTypeOfUseEntity>(entity =>
        {
            entity.ToTable("SubTypeOfUseMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description);
            entity.Property(x => x.TypeOfUseId);
            entity.Property(x => x.SearchSequence);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);

            entity.HasOne(e => e.TypeOfUse)
              .WithMany(c => c.SubTypeOfUse)
              .HasForeignKey(e => e.TypeOfUseId)
              .OnDelete(DeleteBehavior.Restrict);

            // Configure relationships
            entity.HasMany(e => e.PropertyDetails)
                  .WithOne(r => r.SubTypeOfUse)
                  .HasForeignKey(r => r.SubTypeOfUseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.UseFactorCVMaster)
                  .WithOne(r => r.SubTypeOfUse)
                  .HasForeignKey(r => r.SubTypeOfUseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TypeOfUseEntity>(entity =>
        {
            entity.ToTable("TypeOfUseMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TypeOfUseCode);
            entity.Property(x => x.Description);
            entity.Property(x => x.Type);
            entity.Property(x => x.TypeOfUseGroupId);
            entity.Property(x => x.TypeOfUseGroupCVId);
            entity.Property(x => x.SearchSequence);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedDate);
            entity.HasOne(e => e.TypeOfUseGroup)
            .WithMany(p => p.TypeOfUse)
            .HasForeignKey(e => e.TypeOfUseGroupId)
            .HasPrincipalKey(e => e.Id);

            entity.HasOne(e => e.TypeOfUseGroupCV)
           .WithMany(p => p.TypeOfUse)
           .HasForeignKey(e => e.TypeOfUseGroupCVId)
           .HasPrincipalKey(e => e.Id);

            // Configure relationships
            entity.HasMany(e => e.PropertyDetails)
                .WithOne(r => r.TypeOfUse)
                .HasForeignKey(r => r.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.UseFactorCVMaster)
                .WithOne(r => r.TypeOfUse)
                .HasForeignKey(r => r.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ParkingTypeMaster)
                .WithOne(r => r.TypeOfUse)
                .HasForeignKey(r => r.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TaxPercentageMasterCV)
                .WithOne(r => r.TypeOfUse)
                .HasForeignKey(r => r.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertyDescriptionAndTypeOfUseValidation)
                 .WithOne(r => r.TypeOfUse)
                 .HasForeignKey(r => r.TypeOfUseId)
                 .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.SubTypeOfUse)
                .WithOne(r => r.TypeOfUse)
                .HasForeignKey(r => r.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TaxPercentageMasterRV)
             .WithOne(r => r.TypeOfUse)
             .HasForeignKey(r => r.TypeOfUseId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TypeOfUseGroupEntity>(entity =>
        {
            entity.ToTable("TypeOfUseGroupMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TypeOfUseGroupCode);
            entity.Property(x => x.GroupName);
            entity.Property(x => x.GroupIcon);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);
            entity.Property(x => x.IsActive);

            // Configure relationships
            entity.HasMany(e => e.Rates)
                .WithOne(r => r.TypeOfUseGroup)
                .HasForeignKey(r => r.TypeOfUseGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TypeOfUse)
                .WithOne(n => n.TypeOfUseGroup)
                .HasForeignKey(n => n.TypeOfUseGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ZoneEntity>(entity =>
        {
            entity.ToTable("ZoneMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Description).IsRequired().HasMaxLength(50);
            entity.Property(x => x.ZoneNo);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            // Configure relationships
            entity.HasMany(e => e.Ward)
                .WithOne(r => r.Zone)
                .HasForeignKey(r => r.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // MultilingualResource configuration 
        modelBuilder.Entity<MultilingualResourceEntity>(b =>
        {
            b.ToTable("MultilingualResource", "CORE");
            b.HasKey(x => x.Id);

            b.Property(x => x.Resource).HasColumnName("Resource").HasMaxLength(256).IsRequired();
            b.Property(x => x.Key).HasMaxLength(256).IsRequired();

            b.Property(x => x.en_US).IsRequired();
            b.Property(x => x.hi_IN).IsRequired();
            b.Property(x => x.mr_IN).IsRequired();

            b.Property(x => x.IsActive).IsRequired();
            // Unique constraint on Resource + Key combination
            // Prevents duplicate entries and ensures deterministic lookups
            b.HasIndex(x => new { x.Resource, x.Key })
                .IsUnique();
        });
        modelBuilder.Entity<RateMasterForCVEntity>(entity =>
        {
            entity.ToTable("RateCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id);
            entity.Property(e => e.SubZoneId);
            entity.Property(e => e.TypeOfUseGroupCVId);
            entity.Property(e => e.FloorGroupId);
            entity.Property(e => e.RateAmount);
            entity.Property(e => e.AssessmentYearRangeId);
            entity.Property(e => e.IsActive);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()").IsRequired();
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasOne(e => e.FloorGroup).WithMany().HasForeignKey(e => e.FloorGroupId).HasConstraintName("FK_RateCVMaster_FloorGroupMaster");
        });


        modelBuilder.Entity<DepreciationMasterEntity>(entity =>
        {
            entity.ToTable("DepreciationMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConstructionTypeId).HasMaxLength(7);
            entity.Property(e => e.MinYear);
            entity.Property(e => e.MaxYear);
            entity.Property(e => e.Rate).HasColumnType("money");
            entity.Property(e => e.YearRangeRVId);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.ConstructionType)
              .WithMany(c => c.DepreciationMaster)
              .HasForeignKey(e => e.ConstructionTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssessmentYearRange)
                 .WithMany(c => c.DepreciationMaster)
               .HasForeignKey(e => e.YearRangeRVId)
                 .OnDelete(DeleteBehavior.Restrict);
        });
        // TaxZone configuration
        modelBuilder.Entity<TaxZoneEntity>(entity =>
        {
            entity.ToTable("TaxZoneMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TaxZoneNo).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TaxZoneType).HasMaxLength(50);
            entity.Property(e => e.Remark).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.TaxZoneNo).IsUnique().HasDatabaseName("UQ_TaxZoneMaster_TaxZoneNo");

            entity.HasMany(e => e.Rates)
              .WithOne(r => r.TaxZone)
               .HasForeignKey(r => r.TaxZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Property)
                .WithOne(n => n.TaxZone)
                .HasForeignKey(n => n.TaxZoneId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TaxPercentageMasterRV configuration
        modelBuilder.Entity<TaxPercentageMasterRVEntity>(entity =>
        {
            entity.ToTable("TaxPercentageMasterRV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.YearRangeRVId).IsRequired();
            entity.Property(e => e.TypeOfUseId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.YearRangeRVId);
            entity.HasIndex(e => e.TypeOfUseId);

            entity.HasOne(e => e.TypeOfUse)
             .WithMany(c => c.TaxPercentageMasterRV)
             .HasForeignKey(e => e.TypeOfUseId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssessmentYearRange)
             .WithMany(c => c.TaxPercentageMasterRV)
             .HasForeignKey(e => e.YearRangeRVId)
             .OnDelete(DeleteBehavior.Restrict);

        });

        // UserRoleMaster configuration
        modelBuilder.Entity<UserRoleMasterEntity>(entity =>
        {
            entity.ToTable("UserRoleMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserRoleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.UserRoleName).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });


        modelBuilder.Entity<MoujaEntity>(entity =>
        {
            entity.ToTable("MoujaMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MoujaNo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MoujaName).IsRequired().HasMaxLength(100);
            // Configure relationships
            entity.HasMany(e => e.Property)
                .WithOne(r => r.Mouja)
                .HasForeignKey(r => r.MoujaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.MoujaNo).IsUnique().HasDatabaseName("UQ_MoujaMaster_MoujaNo");
            entity.HasIndex(e => e.MoujaName).IsUnique().HasDatabaseName("UQ_MoujaMaster_MoujaName");
        });

        modelBuilder.Entity<SubZoneDetailsForCVEntity>(entity =>
        {
            entity.ToTable("SubZoneDetailsForCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MoujaId).IsRequired();
            entity.Property(e => e.SubZoneNo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SubZoneName).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("getdate()");

            // Foreign key relationship
            entity.HasOne(e => e.Mouja)
                .WithMany()
                .HasForeignKey(e => e.MoujaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SubZoneDetailsForCV_MoujaMaster");

            // Unique constraint on MoujaId + SubZoneNo
            entity.HasIndex(e => new { e.MoujaId, e.SubZoneNo })
                .IsUnique()
                .HasDatabaseName("UQ_SubZoneDetailsForCV_Mouja_SubZoneNo");
        });

        modelBuilder.Entity<BankMasterEntity>(entity =>
        {
            entity.ToTable("BankMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BankCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BranchName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IFSCCode).IsRequired().HasMaxLength(11);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.State).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Pincode).IsRequired().HasMaxLength(6);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.BankCode).IsUnique();
            entity.HasIndex(e => e.IFSCCode).IsUnique();
            entity.HasIndex(e => e.BankName);
            entity.HasIndex(e => e.IsActive);
        });

        // PropertyRuleEvaluationMaster configuration
        modelBuilder.Entity<PropertyRuleEvaluationMasterEntity>(entity =>
        {
            entity.ToTable("PropertyRuleEvaluationMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParameterCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.ParameterCode).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<PolicyConfigurationEntity>(entity =>
        {
            entity.ToTable("PolicyConfiguration", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.PolicyCode).IsUnique();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(20).HasDefaultValue("bit");
            entity.Property(e => e.PolicyValue).HasMaxLength(500);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.AllowedValues).HasMaxLength(500);
            entity.Property(e => e.Unit).HasMaxLength(30);
            entity.Property(e => e.EffectiveFrom);
            entity.Property(e => e.EffectiveTo);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<RoleWiseScreenAccessMasterEntity>(entity =>
        {
            entity.ToTable("RoleWiseScreenAccessMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserRoleId).IsRequired();
            entity.Property(e => e.ScreenId).IsRequired();
            entity.Property(e => e.CanView).IsRequired();
            entity.Property(e => e.CanEdit).IsRequired();
            entity.Property(e => e.CanDelete).IsRequired();
            entity.Property(e => e.HaveFullAccess).IsRequired();
            entity.Property(e => e.HaveNoAccess).IsRequired();
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Foreign key relationships
            entity.HasOne(e => e.UserRole)
                  .WithMany()
                  .HasForeignKey(e => e.UserRoleId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            entity.HasOne(e => e.Screen)
                  .WithMany()
                  .HasForeignKey(e => e.ScreenId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Unique constraint on UserRoleId + ScreenId combination (filtered for active records only)
            // This allows re-creating a role-screen access after it was soft-deleted
            entity.HasIndex(e => new { e.UserRoleId, e.ScreenId })
                  .IsUnique()
                  .HasFilter("[IsActive] = 1")
                  .HasDatabaseName("UX_RoleWiseScreenAccess_UserRole_Screen_Active");

            // Optimized filtered indexes for query performance
            entity.HasIndex(e => new { e.UserRoleId, e.IsActive })
                  .HasFilter("[IsActive] = 1")
                  .HasDatabaseName("IX_RoleWiseScreenAccess_UserRole_Active");

            entity.HasIndex(e => new { e.ScreenId, e.IsActive })
                  .HasFilter("[IsActive] = 1")
                  .HasDatabaseName("IX_RoleWiseScreenAccess_Screen_Active");
        });

        modelBuilder.Entity<YearMasterEntity>(entity =>
        {
            entity.ToTable("YearMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.YearCode).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.StartDate);
            entity.Property(e => e.EndDate);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.YearCode).IsUnique();
        });

        modelBuilder.Entity<OfficeEntity>(entity =>
        {
            entity.ToTable("OfficeMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OfficeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OfficeName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Pincode).IsRequired().HasMaxLength(6);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.EmailId).HasMaxLength(200);
            entity.Property(e => e.OfficeIncharge);
            entity.Property(e => e.DesignationMasterId);
            entity.Property(e => e.EstablishedDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.OfficeCode).IsUnique();
            entity.HasIndex(e => e.OfficeName);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<RateSectionEntity>(entity =>
        {
            entity.ToTable("RateSectionMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description);

            entity.HasMany(e => e.Rates)
               .WithOne(r => r.RateSection)
               .HasForeignKey(r => r.RateSectionId)
               .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RateSectionDetails)
                .WithOne(n => n.RateSection)
                .HasForeignKey(n => n.RateSectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RateSectionDetailsEntity>(entity =>
        {
            entity.ToTable("RateSectionDetails", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RateSectionId);
            entity.Property(x => x.Id);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);
            entity.Property(x => x.IsActive);

            entity.HasOne(e => e.Ward)
              .WithMany(c => c.RateSectionDetails)
              .HasForeignKey(e => e.WardId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RateSection)
              .WithMany(c => c.RateSectionDetails)
              .HasForeignKey(e => e.RateSectionId)
               .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ScreenMasterEntity>(entity =>
        {
            entity.ToTable("ScreenMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ScreenGroupId).HasColumnName("ScreenGroupId");
            entity.Property(e => e.ModuleId).HasColumnName("ModuleId");

            // Required unique properties
            entity.Property(e => e.ScreenCode)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.ScreenName)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(e => e.ScreenGroup)
                .WithMany()
                .HasForeignKey(e => e.ScreenGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Module)
                .WithMany()
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraints
            entity.HasIndex(e => e.ScreenCode)
                .IsUnique()
                .HasFilter("[ScreenCode] IS NOT NULL")
                .HasDatabaseName("IX_ScreenMaster_ScreenCode_Unique");
            entity.HasIndex(e => e.ScreenName)
                .IsUnique()
                .HasFilter("[ScreenName] IS NOT NULL")
                .HasDatabaseName("IX_ScreenMaster_ScreenName_Unique");
        });


        modelBuilder.Entity<ScreenGroupMasterEntity>(entity =>
        {
            entity.ToTable("ScreenGroupMaster", "Core");
            entity.HasKey(e => e.Id);

            // Required unique properties
            entity.Property(e => e.ScreenGroupCode)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.ScreenGroupName)
                .IsRequired()
                .HasMaxLength(200);

            // Unique constraints
            entity.HasIndex(e => e.ScreenGroupCode)
                .IsUnique()
                .HasDatabaseName("IX_ScreenGroupMaster_ScreenGroupCode_Unique");
            entity.HasIndex(e => e.ScreenGroupName)
                .IsUnique()
                .HasDatabaseName("IX_ScreenGroupMaster_ScreenGroupName_Unique");
        });
        modelBuilder.Entity<ModuleMasterEntity>(entity =>
        {
            entity.ToTable("ModuleMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired();
            entity.Property(e => e.ModuleCode)
                .HasMaxLength(50).IsRequired()
                .IsRequired();
            entity.Property(e => e.ModuleName)
                .HasMaxLength(200).IsRequired()
                .IsRequired();
            entity.Property(e => e.ModuleNameLocal).HasMaxLength(200);
            entity.Property(e => e.ModuleIcon).HasMaxLength(100);
            entity.Property(e => e.ModuleLabel).HasMaxLength(100);
            entity.Property(e => e.ModuleDescription).HasMaxLength(500);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Configure relationship with DepartmentMaster
            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepartmentLicenceDetailsEntity>(entity =>
        {
            entity.ToTable("DepartmentLicenceDetails", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LicenceDuration).HasMaxLength(50);
            // Configure relationship with DepartmentMaster
            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DepartmentMasterEntity>(entity =>
        {
            entity.ToTable("DepartmentMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DepartmentCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DepartmentNameLocal).HasMaxLength(200);
            entity.Property(e => e.DepartmentIcon).HasMaxLength(100);
            entity.Property(e => e.DepartmentDescription).HasMaxLength(500);
            // Indexes
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.IsActive);
        });

        // DesignationMasterEntity configuration
        modelBuilder.Entity<DesignationMasterEntity>(entity =>
        {
            entity.ToTable("DesignationMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("Id");
            entity.Property(e => e.DesignationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DesignationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DesignationLocal).HasMaxLength(200);
            entity.Property(e => e.DesignationDescription).HasMaxLength(500);
            // Indexes
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.DesignationCode).IsUnique();
        });



        // ActiveTaxes configuration
        modelBuilder.Entity<ActiveTaxesEntity>(entity =>
        {
            entity.ToTable("ActiveTaxesMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.TaxName).HasMaxLength(200);
            entity.Property(x => x.TaxNameAlias).HasMaxLength(200);
            entity.Property(x => x.DisplayOrder);
            entity.Property(x => x.TaxOnUnit).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);
        });

        // GrievanceCategoryMaster configuration
        modelBuilder.Entity<GrievanceCategoryEntity>(entity =>
        {
            entity.ToTable("GrievanceCategoryMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Id);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResolutionSla).HasMaxLength(100);
            entity.Property(e => e.EscalationLevel).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CategoryCode).IsUnique();
            entity.HasIndex(e => e.CategoryName);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<ULBMasterEntity>(entity =>
        {
            entity.ToTable("ULBMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UlbCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UlbName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UlbNameLocal).HasMaxLength(200);
            entity.Property(e => e.UlbTypeId).IsRequired();
            entity.Property(e => e.UlbLogo).HasMaxLength(500);
            entity.Property(e => e.EmailId).HasMaxLength(200);
            entity.Property(e => e.MobileNo).HasMaxLength(20);
            entity.Property(e => e.AlternateMobileNo).HasMaxLength(20);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(200);
            entity.Property(e => e.ContactPersonName).HasMaxLength(200);
            entity.Property(e => e.ContactPersonDesignation).HasMaxLength(200);
            entity.Property(e => e.UlbAddress).HasMaxLength(500);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.PinCode).HasMaxLength(6);
            entity.Property(e => e.PartnerName).HasMaxLength(200);
            entity.Property(e => e.PMName).HasMaxLength(200);
            entity.Property(e => e.PMEmailId).HasMaxLength(200);
            entity.Property(e => e.PMMobileNo).HasMaxLength(20);
            entity.Property(e => e.LicenceType).HasMaxLength(50);
            entity.Property(e => e.LicenceDuration).HasMaxLength(50);
            entity.Property(e => e.SupportType).HasMaxLength(100);
            entity.Property(e => e.LicenceKey).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();

            // Ignore BaseEntity properties that don't exist in database

            entity.Ignore(e => e.CreatedDate);
            entity.Ignore(e => e.UpdatedDate);
            entity.Ignore(e => e.CreatedBy);
            entity.Ignore(e => e.UpdatedBy);

            // Indexes for better query performance
            entity.HasIndex(e => e.UlbCode).IsUnique();
            entity.HasIndex(e => e.UlbTypeId);
            entity.HasIndex(e => e.IsActive);
        });

        // CombinePropertyHistory configuration
        modelBuilder.Entity<CombinePropertyHistoryEntity>(entity =>
        {
            entity.ToTable("CombinePropertyHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SourcePropertyId).IsRequired();
            entity.Property(e => e.CombinedPropertyId).IsRequired();
            entity.Property(e => e.CombineReason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.SourcePropertyId);
            entity.HasIndex(e => e.CombinedPropertyId);
        });
        // TransMast configuration
        modelBuilder.Entity<TransMastEntity>(entity =>
        {
            entity.ToTable("TransMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.RVorCVValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            entity.HasOne(e => e.Property)
                    .WithMany(p => p.TransMast)
                    .HasForeignKey(e => e.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);

        });

        // TaxPendingDetails configuration
        modelBuilder.Entity<TaxPendingDetailsEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PendingYearId).IsRequired();
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.PendingAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PendingFixed).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyId);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            entity.HasOne(r => r.PropertyMast)
                .WithMany(p => p.TaxPendingDetails)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PendingYear)
                .WithMany()
                .HasForeignKey(e => e.PendingYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tax)
                .WithMany()
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.PropertyId, e.PendingYearId, e.TaxId });
        });

        modelBuilder.Entity<RoomWiseSubmissionDetailsEntity>(entity =>
        {
            entity.ToTable("RoomWiseSubmissionDetails", "PTIS");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.LengthMtr);
            entity.Property(e => e.WidthMtr);
            entity.Property(e => e.AreaSqMtr);
            entity.Property(e => e.HeightMtr);
            entity.Property(e => e.Base1Mtr);
            entity.Property(e => e.Base2Mtr);
            entity.Property(e => e.TotalAreaSqMtr);
            entity.Property(e => e.Shape).HasMaxLength(100);
            entity.Property(e => e.RoomNo).HasMaxLength(50);
            entity.Property(e => e.RoomTypeId);
            entity.Property(e => e.SubmissionType).HasMaxLength(100);
            entity.Property(e => e.OuterYesNo).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MinusYesNo).IsRequired().HasDefaultValue(false);

            // Audit and soft delete configuration
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasOne(e => e.PropertyDetails)
                  .WithMany(p => p.RoomWiseSubmissionDetails)
                  .HasForeignKey(e => e.PropertyDetailsId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyMast)
                  .WithMany(p => p.RoomWiseSubmissionDetails)
                  .HasForeignKey(e => e.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RoomTypeMaster)
                 .WithMany()
                 .HasForeignKey(e => e.RoomTypeId)
                 .OnDelete(DeleteBehavior.Restrict);
            // Indexes for better query performance
            entity.HasIndex(e => e.PropertyDetailsId);
            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.RoomTypeId);
        });

        // RoomWiseMinusData configuration
        modelBuilder.Entity<RoomWiseMinusDataEntity>(entity =>
        {
            entity.ToTable("RoomWiseMinusData", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoomWiseSubmissionId).IsRequired();
            entity.Property(e => e.LengthMtr);
            entity.Property(e => e.WidthMtr);
            entity.Property(e => e.AreaSqMtr);
            entity.Property(e => e.HeightMtr);
            entity.Property(e => e.Base1Mtr);
            entity.Property(e => e.Base2Mtr);
            entity.Property(e => e.IsOffset).HasDefaultValue(false);
            entity.Property(e => e.Shape).HasMaxLength(25);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.RoomWiseSubmissionId);

            // Configure FK relationship with navigation property to RoomWiseSubmissionDetails
            entity.HasOne(e => e.RoomWiseSubmissionDetails)
                  .WithMany(r => r.PropertyRoomMinus)
                  .HasForeignKey(e => e.RoomWiseSubmissionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        // PropertyDetailsReassessmentEntity configuration
        modelBuilder.Entity<PropertyDetailsReassessmentEntity>(entity =>
        {
            entity.ToTable("PropertyDetailsReassessment", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FloorId).IsRequired();
            entity.Property(e => e.SubFloorId).IsRequired();
            entity.Property(e => e.ConstructionTypeId).IsRequired();
            entity.Property(e => e.TypeOfUseId).IsRequired();
            entity.Property(e => e.SubTypeOfUseId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.FloorId);
            entity.HasIndex(e => e.SubFloorId);
            entity.HasIndex(e => e.ConstructionTypeId);
            entity.HasIndex(e => e.TypeOfUseId);
            entity.HasIndex(e => e.SubTypeOfUseId);
        });

        modelBuilder.Entity<PropertyCategoryEntity>(entity =>
        {
            entity.ToTable("PropertyCategoryMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyCategoryName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyCategoryName).IsUnique().HasDatabaseName("UQ_PropertyCategoryMaster_PropertyCategoryName");
        });

        // PropertyTypeCategory configuration
        modelBuilder.Entity<PropertyTypeCategoryEntity>(entity =>
        {
            entity.ToTable("PropertyTypeCategoryMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyTypeCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyTypeCategory).IsUnique().HasDatabaseName("UQ_PropertyTypeCategoryMaster_PropertyTypeCategory");
        });

        // PropertyTypeMaster configuration
        modelBuilder.Entity<PropertyTypeMasterEntity>(entity =>
        {
            entity.ToTable("PropertyTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyDescription).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(5);
            entity.Property(e => e.SearchSequence);
            entity.Property(e => e.PartType);
            entity.Property(e => e.PropertyTypeCategoryId);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyDescription).IsUnique().HasDatabaseName("UQ_PropertyTypeMaster_PropertyDescription");
        });

        // PropertyPhotoType configuration
        modelBuilder.Entity<PropertyPhotoTypeEntity>(entity =>
        {
            entity.ToTable("PropertyPhotoType", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PhotoTypeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PhotoTypeName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PhotoTypeCode).IsUnique().HasDatabaseName("UQ_PropertyPhotoType_Code");
        });

        // PropertyAssessment configuration (PropertyMastDetails table)
        modelBuilder.Entity<PropertyAssessmentEntity>(entity =>
        {
            entity.ToTable("PropertyMastDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.OwnerTypeId);
            entity.Property(e => e.AssessmentRemark).HasMaxLength(400);
            entity.Property(e => e.SurveyRemark).HasMaxLength(400);
            entity.Property(e => e.FlatSystemRemark).HasMaxLength(400);
            entity.Property(e => e.CombPropRemark).HasMaxLength(400);
            entity.Property(e => e.AdharCardNo).HasMaxLength(12);
            entity.Property(e => e.RenterMobileNo).HasMaxLength(13);
            entity.Property(e => e.PrarupYadiPublishDate);
            entity.Property(e => e.AntimYadiPublishDate);
            entity.Property(e => e.PropertyRegDate);
            entity.Property(e => e.ApplyTaxesFrom);
            entity.Property(e => e.PartOCDate);
            entity.Property(e => e.BHK).HasMaxLength(50);
            entity.Property(e => e.BlockNo).HasMaxLength(20);
            entity.Property(e => e.AlternativeEmailId).HasColumnName("AlternetivEmailId").HasMaxLength(100);
            entity.Property(e => e.TotalBuiltupAreaSqFeet);
            entity.Property(e => e.TotalBuiltupAreaSqMeter);
            entity.Property(e => e.Latitude).HasMaxLength(20);
            entity.Property(e => e.Longitude).HasMaxLength(20);
            entity.Property(e => e.NoOfResidentialToilets);
            entity.Property(e => e.NoOfCommercialToilets);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasIndex(e => e.PropertyId);

            entity.HasOne(r => r.PropertyMast)
                .WithMany(p => p.PropertyMastDetails)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // According to the actual database schema, WingNo does NOT exist in PropertyMastDetails table
            // WingNo is stored in SocietyDetailsMast table instead
            entity.Ignore(e => e.WingNo);
        });

        // PropertyDetails configuration
        modelBuilder.Entity<PropertyDetailsEntity>(entity =>
        {
            entity.ToTable("PropertyDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.CarpetAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.BuiltupAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.CarpetAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.BuiltupAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.ConstructionYear).HasColumnType("varchar(4)");
            entity.Property(e => e.AssessmentYear).HasColumnType("nvarchar(4)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasIndex(e => e.Id);

            entity.HasOne(e => e.Property)
                   .WithMany(p => p.PropertyDetails)
                    .HasForeignKey(e => e.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Floor)
                  .WithMany(p => p.PropertyDetails)
                  .HasForeignKey(e => e.FloorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SubFloor)
                  .WithMany(p => p.PropertyDetails)
                  .HasForeignKey(e => e.SubFloorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConstructionType)
                  .WithMany(p => p.PropertyDetails)
                  .HasForeignKey(e => e.ConstructionTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TypeOfUse)
                  .WithMany(p => p.PropertyDetails)
                  .HasForeignKey(e => e.TypeOfUseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SubTypeOfUse)
                  .WithMany(p => p.PropertyDetails)
                  .HasForeignKey(e => e.SubTypeOfUseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RoomWiseSubmissionDetails)
                  .WithOne(r => r.PropertyDetails)
                  .HasForeignKey(r => r.PropertyDetailsId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RenterDetails)
                  .WithOne(r => r.PropertyDetails)
                  .HasForeignKey(r => r.PropertyDetailsId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Renters)
                  .WithOne(r => r.PropertyDetails)
                  .HasForeignKey(r => r.PropertyDetailsId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PlotDetails configuration
        modelBuilder.Entity<PlotDetailsEntity>(entity =>
        {
            entity.ToTable("PlotDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId);
            entity.Property(e => e.PlotArea).HasColumnType("float");
            entity.Property(e => e.PlotTaxableAreaSqFt).HasColumnType("float");
            entity.Property(e => e.OpenPlotType).HasMaxLength(10);
            entity.Property(e => e.OpenPlotRenterName).HasMaxLength(1000);
            entity.Property(e => e.OpenPlotLength).HasColumnType("float");
            entity.Property(e => e.OpenPlotWidth).HasColumnType("float");
            entity.Property(e => e.PlotTaxableAreaSqMtr).HasColumnType("float");
            entity.Property(e => e.PlotAreaSqMtr).HasColumnType("float");
            entity.Property(e => e.OpenPlotSubmissionType).HasColumnType("varchar(30)");
            entity.Property(e => e.PlotAreaMtrLength).HasColumnType("float");
            entity.Property(e => e.PlotAreaMtrWidth).HasColumnType("float");
            entity.Property(e => e.PlotAreaFtLength).HasColumnType("float");
            entity.Property(e => e.PlotAreaFtWidth).HasColumnType("float");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            entity.HasOne(e => e.PropertyMast)
                  .WithMany(p => p.PlotDetails)
                  .HasForeignKey(e => e.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // SocietyDetails configuration
        modelBuilder.Entity<SocietyDetailsEntity>(entity =>
        {
            entity.ToTable("SocietyDetailsMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Id);
            entity.Property(e => e.WingId);
            entity.Property(e => e.WingName).HasMaxLength(100);

            entity.HasOne<WingEntity>()
                .WithMany()
                .HasForeignKey(e => e.WingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.SocietyName).HasMaxLength(500);
            entity.Property(e => e.SocietyAddress).HasMaxLength(200);
            entity.Property(e => e.SecretaryName).HasMaxLength(200);
            entity.Property(e => e.ManagerName).HasMaxLength(200);
            entity.Property(e => e.LandOwnerName).HasMaxLength(200);
            entity.Property(e => e.BuilderName).HasMaxLength(200);
            entity.Property(e => e.SocietyNameEnglish).HasMaxLength(500);
            entity.Property(e => e.SocietyAddressEnglish).HasMaxLength(200);
            entity.Property(e => e.SecretaryNameEnglish).HasMaxLength(200);
            entity.Property(e => e.ManagerNameEnglish).HasMaxLength(200);
            entity.Property(e => e.LandOwnerNameEnglish).HasMaxLength(200);
            entity.Property(e => e.BuilderNameEnglish).HasMaxLength(200);
            entity.Property(e => e.ManagerMobileNo).HasMaxLength(13);
            entity.Property(e => e.ManagerMobileNoRemarkId);
            entity.Property(e => e.SecretaryMobileNo).HasMaxLength(13);
            entity.Property(e => e.SecretaryMobileNoRemarkId);
            entity.Property(e => e.BuilderMobileNo).HasMaxLength(13);
            entity.Property(e => e.BuilderMobileNoRemarkId);
            entity.Property(e => e.SocietyEmailId).HasMaxLength(100);
            entity.Property(e => e.SecretaryEmailId).HasMaxLength(100);
            entity.Property(e => e.ManagerEmailId).HasMaxLength(100);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Configure foreign key relationships
            entity.HasOne(r => r.PropertyMast)
             .WithMany(p => p.SocietyDetailsMast)
             .HasForeignKey(r => r.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.ManagerMobileNoRemarkId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.SecretaryMobileNoRemarkId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.BuilderMobileNoRemarkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Property configuration
        modelBuilder.Entity<PropertyEntity>(entity =>
        {
            entity.ToTable("PropertyMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyNo).HasMaxLength(10);
            entity.Property(e => e.PartitionNo).HasMaxLength(10);
            // entity.Property(e => e.Id);
            entity.Property(e => e.UPICId).HasMaxLength(30);
            entity.Property(e => e.OpenPlot);
            entity.Property(e => e.CSN).HasMaxLength(30);
            entity.Property(e => e.SubZoneNo).HasMaxLength(20);
            entity.Property(e => e.PlotNo).HasMaxLength(20);
            entity.Property(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(5);
            entity.Property(e => e.OwnerTitle).HasMaxLength(20);
            entity.Property(e => e.OwnerName).HasMaxLength(1000);
            entity.Property(e => e.OccupierTitle).HasMaxLength(20);
            entity.Property(e => e.OccupierName).HasMaxLength(1000);
            entity.Property(e => e.FlatOrShopNo).HasMaxLength(100);
            entity.Property(e => e.FlatOrShopName).HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.MobileNo).HasMaxLength(13);
            entity.Property(e => e.EmailId).HasMaxLength(100);
            // entity.Property(e => e.Id);
            entity.Property(e => e.OwnerTitleEnglish).HasMaxLength(20);
            entity.Property(e => e.OwnerNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.OccupierTitleEnglish).HasMaxLength(20);
            entity.Property(e => e.OccupierNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.FlatOrShopNoEnglish).HasMaxLength(100);
            entity.Property(e => e.FlatOrShopNameEnglish).HasMaxLength(200);
            entity.Property(e => e.AddressEnglish).HasMaxLength(500);
            entity.Property(e => e.LocationEnglish).HasMaxLength(200);
            entity.Property(e => e.PinCode).HasMaxLength(6).HasColumnType("varchar(6)");
            entity.Property(e => e.MobileNoRemarkId);
            entity.Property(e => e.AlternateMobileNo).HasMaxLength(13).HasColumnType("varchar(13)");
            entity.Property(e => e.OccupierMobileNo).HasMaxLength(13).HasColumnType("varchar(13)");
            entity.Property(e => e.OccupierMobileNoRemarkId);
            entity.Property(e => e.PropertyAssessmentStatusId);
            entity.Property(e => e.PropertyMastOldId);
            entity.Property(e => e.PropertyFloorId);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasMany(e => e.SocietyDetailsMast)
              .WithOne(d => d.PropertyMast)
              .HasForeignKey(d => d.PropertyId)
              .OnDelete(DeleteBehavior.Restrict);

            // Configure PropertyMastDetails (PropertyAssessmentEntity) relationship
            // This prevents EF Core from generating a shadow PropertyEntityId foreign key
            entity.HasMany(e => e.PropertyMastDetails)
                .WithOne(pmd => pmd.PropertyMast)
                .HasForeignKey(pmd => pmd.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertyTaxCalculationCVResults)
                .WithOne(d => d.PropertyMast)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertyTaxCalculationRVResults)
                .WithOne(d => d.PropertyMast)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.FlagMaster)
                .WithOne(d => d.PropertyMast)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TransMastCV)
              .WithOne(d => d.PropertyMast)
              .HasForeignKey(d => d.PropertyId)
              .OnDelete(DeleteBehavior.Restrict);

            // Configure PlotDetails relationship
            entity.HasMany(e => e.PlotDetails)
                .WithOne(pd => pd.PropertyMast)
                .HasForeignKey(pd => pd.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure foreign key relationships for the new columns
            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.MobileNoRemarkId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.OccupierMobileNoRemarkId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PropertyMastOldEntity>()
                .WithMany()
                .HasForeignKey(e => e.PropertyMastOldId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyAssessmentStatus)
                .WithMany()
                .HasForeignKey(e => e.PropertyAssessmentStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // PropertyDetailsOld does NOT have PropertyId in database - it's related to PropertyMastOld only
            // Ignore this navigation to prevent shadow PropertyId/PropertyEntityId creation
            entity.Ignore(e => e.PropertyDetailsOld);

            // PropertyMastOld relationship is via PropertyMastOldId FK in PropertyEntity
            // Ignore collection navigation to prevent confusion
            entity.Ignore(e => e.PropertyMastOld);

            // Unique index on WardId, PropertyNo, PartitionNo
            entity.HasIndex(e => new { e.WardId, e.PropertyNo, e.PartitionNo })
                .IsUnique()
                .HasFilter("[PropertyNo] IS NOT NULL AND [PartitionNo] IS NOT NULL")
                .HasDatabaseName("UQ_Property_Ward_Property_Partition");

            entity.HasOne(e => e.Ward)
              .WithMany(c => c.Property)
              .HasForeignKey(e => e.WardId)
              .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Mouja)
             .WithMany(c => c.Property)
             .HasForeignKey(e => e.MoujaId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TaxZone)
             .WithMany(c => c.Property)
             .HasForeignKey(e => e.TaxZoneId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PropertySocialDetails)
                .WithOne(psd => psd.PropertyMast)
                .HasForeignKey(psd => psd.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PropertyCertificates relationship to prevent shadow PropertyEntityId FK
            entity.HasMany(e => e.PropertyCertificates)
                .WithOne()
                .HasForeignKey(pc => pc.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<ConfigCategoryMasterEntity>(entity =>
        {
            entity.ToTable("ConfigCategoryMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CategoryCode).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.HasIndex(e => e.CategoryCode).IsUnique();

        });
        modelBuilder.Entity<ConfigKeyMasterEntity>(entity =>
        {
            entity.ToTable("ConfigKeyMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigCode).IsRequired().HasMaxLength(60);
            entity.Property(e => e.ConfigName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.DataType).HasMaxLength(20);
            entity.Property(e => e.ControlType).HasMaxLength(30);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);

            // Foreign Key Relationship
            entity.HasOne(e => e.Category)
                .WithMany(c => c.ConfigKeys)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.ConfigCode).IsUnique();
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<PaymentModeEntity>(entity =>
        {
            entity.ToTable("PaymentMode", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code);
            entity.Property(e => e.PaymentModeName);
            entity.Property(e => e.Type);
            entity.Property(e => e.Category);
            entity.Property(e => e.Description);
            entity.Property(e => e.ChargeType);
            entity.Property(e => e.TransactionCharge);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);

        });
        modelBuilder.Entity<ConfigValueMasterEntity>(entity =>
        {
            entity.ToTable("ConfigValueMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasMaxLength(500);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.UpdatedBy);

            // Foreign Key Relationships
            entity.HasOne(e => e.ConfigKey)
                .WithMany()
                .HasForeignKey(e => e.ConfigKeyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Module)
                .WithMany()
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.ConfigKeyId);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.ModuleId);
            entity.HasIndex(e => e.IsActive);
        });




        // ── UserMaster ───────────────────────────────────────────────────────
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("UserMaster", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Identity
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);

            // Profile — Name split into parts
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.Property(e => e.UserCode).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(400);
            entity.Property(e => e.MobileNo).HasMaxLength(30);
            entity.Property(e => e.AlternateMobileNo).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.MustChangePassword).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.Remark).HasMaxLength(400);
            entity.Property(e => e.EmployeeTypeID);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // IHardDeletable
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            // Auth tracking — owned by auth flow, never exposed in DTOs
            // entity.Property(e => e.UserNameNormalized).HasMaxLength(100);
            entity.Property(e => e.FailedLoginCount).HasDefaultValue(0);
            entity.Property(e => e.LockedUntilAt);
            entity.Property(e => e.LastLoginAt);

            // Security
            entity.Property(e => e.PasswordHash).HasMaxLength(255);

            // Audit
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Indexes
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.UserCode);
            //entity.HasIndex(e => e.UserNameNormalized).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsActive);
        });


        // RefreshToken configuration
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("RefreshToken", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.RevokedAt);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.IsRevoked, e.ExpiresAt });
        });

        modelBuilder.Entity<WingEntity>(entity =>
        {
            entity.ToTable("WingMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WingNo).IsRequired().HasMaxLength(10);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedDate);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.UpdatedBy);
        });


        // PropertyMastOld configuration
        modelBuilder.Entity<PropertyMastOldEntity>(entity =>
        {
            entity.ToTable("PropertyMastOld", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OldWardNo).HasMaxLength(10);
            entity.Property(e => e.OldPropertyNo).HasMaxLength(10);
            entity.Property(e => e.OldPartitionNo).HasMaxLength(10);
            entity.Property(e => e.OldEgovNo).HasMaxLength(10);
            entity.Property(e => e.OldPropertyTypeId);
            entity.Property(e => e.OldALV).HasColumnType("float");
            entity.Property(e => e.OldRV).HasColumnType("float");
            entity.Property(e => e.OldGeneralTax).HasColumnType("float");
            entity.Property(e => e.OldTotalTax).HasColumnType("float");
            entity.Property(e => e.OldZoneNo).HasMaxLength(20);
            entity.Property(e => e.OldPlotNo).HasMaxLength(20);
            entity.Property(e => e.OldCSN).HasMaxLength(30);
            entity.Property(e => e.OldPlotArea).HasColumnType("float");
            entity.Property(e => e.OldAssessmentYear);
            entity.Property(e => e.OldFloor).HasMaxLength(10);
            entity.Property(e => e.OldConstructionTypeOfUseId).HasMaxLength(7);
            entity.Property(e => e.OldUseType).HasMaxLength(100);
            entity.Property(e => e.OldConstructionArea).HasColumnType("float");
            entity.Property(e => e.OldOwnerName).HasMaxLength(1000);
            entity.Property(e => e.OldOccupierName).HasMaxLength(1000);
            entity.Property(e => e.OldAddress).HasMaxLength(500);
            entity.Property(e => e.OldOwnerNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.OldOccupierNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.OldAddressEnglish).HasMaxLength(500);
            entity.Property(e => e.NoOfOldToilets);
            entity.Property(e => e.OldTotalRooms);
            entity.Property(e => e.OldSocietyName).HasMaxLength(300);
            entity.Property(e => e.OldEmailId).HasMaxLength(100);
            entity.Property(e => e.OldParkingAreaSqFt).HasColumnType("float");
            entity.Property(e => e.OldParkingAreaSqMtr).HasColumnType("float");
            entity.Property(e => e.OldAssessmentDate);
            entity.Property(e => e.OldFlatOrShopNumber).HasMaxLength(50);
            entity.Property(e => e.OldWing).HasMaxLength(20);
            entity.Property(e => e.OldMobileNo).HasMaxLength(13);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
        });

        // PropertyDetailsOld configuration
        modelBuilder.Entity<PropertyDetailsOldEntity>(entity =>
        {
            entity.ToTable("PropertyDetailsOld", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyMastOldId).IsRequired();
            entity.Property(e => e.OldFloorId);
            entity.Property(e => e.OldSubFloorId);
            entity.Property(e => e.OldConstructionYear).HasMaxLength(4);
            entity.Property(e => e.OldAssessmentYear).HasMaxLength(4);
            entity.Property(e => e.OldConstructionTypeId);
            entity.Property(e => e.OldTypeOfUseId);
            entity.Property(e => e.OldSubTypeOfUseId);
            entity.Property(e => e.OldCarpetAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.OldCarpetAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.OldBuiltupAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.OldBuiltupAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyMastOldId);

            // Configure FK relationship to PropertyMastOld
            entity.HasOne<PropertyMastOldEntity>()
                .WithMany()
                .HasForeignKey(e => e.PropertyMastOldId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // RefreshToken configuration
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("RefreshToken", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.RevokedAt);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.IsRevoked, e.ExpiresAt });
        });


        modelBuilder.Entity<PropertyTaxCalculationCVResultsEntity>(entity =>
        {
            entity.ToTable("PropertyTaxCalculationCVResults", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Relationship to Property - Restrict delete to preserve CV history
            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertyTaxCalculationCVResults)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Explicitly configure the RateCVMaster relationship
            entity.HasOne(e => e.RateCVMaster)
                .WithMany()
                .HasForeignKey(e => e.RateCVMasterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to PropertyDetails - Restrict delete to preserve CV history
            entity.HasOne(e => e.PropertyDetails)
                .WithMany(p => p.PropertyTaxCalculationCVResults)
                .HasForeignKey(e => e.PropertyDetailsId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to RateCVMaster - Restrict delete to preserve CV history
            entity.HasOne(e => e.RateCVMaster)
                .WithMany()
                .HasForeignKey(e => e.RateCVMasterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to TaxMaster - Restrict delete
            entity.HasOne(e => e.TaxMaster)
                .WithMany()
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to FloorFactorCVMaster - Restrict delete
            entity.HasOne(e => e.FloorFactorCVMaster)
                .WithMany()
                .HasForeignKey(e => e.FloorFactorCVId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to AgeFactorCVMaster - Restrict delete
            entity.HasOne(e => e.AgeFactorCVMaster)
                .WithMany()
                .HasForeignKey(e => e.AgeFactorCVId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to NatureFactorCVMaster - Restrict delete
            entity.HasOne(e => e.NatureFactorCVMaster)
                .WithMany()
                .HasForeignKey(e => e.NatureFactorCVId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to UseFactorCVMaster - Restrict delete
            entity.HasOne(e => e.UseFactorCVMaster)
                .WithMany()
                .HasForeignKey(e => e.UseFactorCVId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => new { e.PropertyId, e.PropertyDetailsId, e.TaxId })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UQ_PropertyTaxCalculationCVResults_Property_PropertyDetails_Tax_Active");
            entity.HasIndex(e => e.RateCVMasterId)
                .HasDatabaseName("IX_PropertyTaxCalculationCVResults_RateCVMasterId");
            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.PropertyDetailsId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.MarkedForDeletion);
        });

        // ── UserDepartmentAllocation ─────────────────────────────────────────
        modelBuilder.Entity<UserDepartmentAllocationEntity>(entity =>
        {
            entity.ToTable("UserDepartmentAllocation", "CORE");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DepartmentId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasOne(e => e.Department)
                  .WithMany()
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DepartmentId }).IsUnique();
        });


        modelBuilder.Entity<RenterDetailEntity>(entity =>
        {
            entity.ToTable("RenterDetails", "PTIS");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgreementId).HasMaxLength(100);
            entity.Property(e => e.IncrementFrequency).HasMaxLength(50);
            entity.Property(e => e.IncrementType).HasMaxLength(50);
            entity.Property(e => e.IncrementMethod).HasMaxLength(50);
            entity.Property(e => e.IncrementValue);
            entity.Property(e => e.RentAmount);
            entity.Property(e => e.RentMonthly);
            entity.Property(e => e.Increment);

            // Custom increment fields
            entity.Property(e => e.CustomFromDate);
            entity.Property(e => e.CustomToDate);
            entity.Property(e => e.CustomIncrementType).HasMaxLength(50);
            entity.Property(e => e.CustomIncrementValue);
            entity.Property(e => e.CustomMethod).HasMaxLength(50);

            // Audit and soft delete configuration
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasOne(e => e.PropertyDetails)
                  .WithMany(p => p.RenterDetails)
                  .HasForeignKey(e => e.PropertyDetailsId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes for better query performance
            entity.HasIndex(e => e.PropertyDetailsId);
        });




        // ── UserModuleAllocation ─────────────────────────────────────────────
        modelBuilder.Entity<UserModuleAllocationEntity>(entity =>
        {
            entity.ToTable("UserModuleAllocation", "CORE");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DepartmentId).IsRequired();
            entity.Property(e => e.ModuleId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);


            entity.HasOne(e => e.Department)
                  .WithMany()
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasOne(e => e.Module)
                  .WithMany()
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DepartmentId, e.ModuleId }).IsUnique();
        });

        // ── UserRoleAllocation ───────────────────────────────────────────────
        modelBuilder.Entity<UserRoleAllocationEntity>(entity =>
        {
            entity.ToTable("UserRoleAllocation", "CORE");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DepartmentId).IsRequired();
            entity.Property(e => e.UserRoleId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.Department)
                  .WithMany()
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasOne(e => e.UserRole)
                  .WithMany()
                  .HasForeignKey(e => e.UserRoleId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DepartmentId, e.UserRoleId }).IsUnique();
        });

        // EmployeeTypeMaster configuration
        modelBuilder.Entity<EmployeeTypeEntity>(entity =>
        {
            entity.ToTable("EmployeeTypeMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<PropertyDescriptionAndTypeOfUseValidationEntity>(entity =>
        {
            entity.ToTable("PropertyDescriptionAndTypeOfUseValidation", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyTypeId).IsRequired();
            entity.Property(e => e.TypeOfUseId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.TypeOfUse)
              .WithMany(c => c.PropertyDescriptionAndTypeOfUseValidation)
              .HasForeignKey(e => e.TypeOfUseId)
              .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on PropertyTypeId and TypeOfUseId combination
            entity.HasIndex(e => new { e.PropertyTypeId, e.TypeOfUseId })
                .IsUnique()
                .HasDatabaseName("UQ_PropertyDescriptionAndTypeOfUseValidation_PropertyTypeId_TypeOfUseId");
        });


        modelBuilder.Entity<PropertyTaxCalculationRVResultsEntity>(entity =>
        {
            entity.ToTable("PropertyTaxCalculationRVResults", "PTIS");
            entity.HasKey(e => e.Id);

            // Previously double? (SQL float). Migrated to decimal(18,4) for financial precision.
            // Migration: 20260609_RVResults_FloatToDecimal applies the matching ALTER COLUMN statements.
            entity.Property(e => e.MonthlyRate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.YearlyRate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.YearlyRent).HasColumnType("decimal(18,4)");
            entity.Property(e => e.AnnualRentalValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalAreaSqMtr).HasColumnType("decimal(18,4)");
            entity.Property(e => e.RAreaSqMtr).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CAreaSqlMtr).HasColumnType("decimal(18,4)");

            entity.Property(e => e.Depreciation).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Maintenance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RateableValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.REducationTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CEducationTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.REducationTaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CEducationTaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.REmploymentTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CEmploymentTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.REmploymentTaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CEmploymentTaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);


            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertyTaxCalculationRVResults)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyDetails)
                .WithMany(p => p.PropertyTaxCalculationRVResults)
                .HasForeignKey(e => e.PropertyDetailsId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // UseFactorCVMaster configuration
        modelBuilder.Entity<UseFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("UseFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.TypeOfUseId).IsRequired();
            entity.Property(e => e.SubTypeOfUseId).IsRequired();
            entity.Property(e => e.Factor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(e => e.TypeOfUse)
                .WithMany(p => p.UseFactorCVMaster)
                .HasForeignKey(e => e.TypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SubTypeOfUse)
                .WithMany(p => p.UseFactorCVMaster)
                .HasForeignKey(e => e.SubTypeOfUseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.YearRangeCV)
                .WithMany(p => p.UseFactorCVMaster)
                .HasForeignKey(e => e.YearRangeCVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TypeOfUseId);
            entity.HasIndex(e => e.SubTypeOfUseId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.IsActive);
            // Uniqueness constraint for TypeOfUseId + SubTypeOfUseId + YearRangeCVId
            entity.HasIndex(e => new { e.TypeOfUseId, e.SubTypeOfUseId, e.YearRangeCVId }).IsUnique();
        });
        // AgeFactorCVMaster configuration
        modelBuilder.Entity<AgeFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("AgeFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConstructionTypeId).IsRequired();
            entity.Property(e => e.AgeFrom).IsRequired();
            entity.Property(e => e.AgeTo).IsRequired();
            entity.Property(e => e.Factor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(e => e.ConstructionType)
                .WithMany(p => p.AgeFactorCVMaster)
                .HasForeignKey(e => e.ConstructionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.YearRangeCV)
                .WithMany(p => p.AgeFactorCVMaster)
                .HasForeignKey(e => e.YearRangeCVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ConstructionTypeId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.AgeFrom);
            entity.HasIndex(e => e.AgeTo);
            entity.HasIndex(e => e.IsActive);
        });

        // FloorFactorCVMaster configuration
        modelBuilder.Entity<FloorFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("FloorFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FloorId).IsRequired();
            entity.Property(e => e.FactorWithLift).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.FactorWithoutLift).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(e => e.Floor)
                .WithMany(p => p.FloorFactorCVMaster)
                .HasForeignKey(e => e.FloorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.YearRangeCV)
                .WithMany(p => p.FloorFactorCVMaster)
                .HasForeignKey(e => e.YearRangeCVId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.FloorId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.IsActive);
            // Uniqueness constraint for FloorId + YearRangeCVId to match service expectations
            // CapitalValueService.CreateAsync uses ToDictionaryAsync((FloorId, YearRangeCVId))
            // which will throw if duplicates exist
            entity.HasIndex(e => new { e.FloorId, e.YearRangeCVId }).IsUnique();
        });

        // FlagMaster configuration
        modelBuilder.Entity<FlagMasterEntity>(entity =>
        {
            entity.ToTable("FlagMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.FlagMaster)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        modelBuilder.Entity<TaxPercentageMasterCVEntity>(entity =>
        {
            entity.ToTable("TaxPercentageMasterCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaxId);
            entity.Property(e => e.TypeOfUseId);
            entity.Property(e => e.YearRangeCVId);
            entity.Property(e => e.TaxPercentage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Foreign key configuration
            entity.HasOne(e => e.TaxMaster)
                .WithMany(p => p.TaxPercentageMasterCV)
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TypeOfUse)
             .WithMany(c => c.TaxPercentageMasterCV)
             .HasForeignKey(e => e.TypeOfUseId)
             .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssessmentYearRangeCV)
            .WithMany(c => c.TaxPercentageMasterCV)
            .HasForeignKey(e => e.YearRangeCVId)
            .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on natural key (TaxId, TypeOfUseId, YearRangeCVId) for active records
            // Prevents duplicate tax percentage configurations that could lead to ambiguous calculations
            entity.HasIndex(e => new { e.TaxId, e.TypeOfUseId, e.YearRangeCVId })
                .IsUnique()
                .HasDatabaseName("UX_TaxPercentageMasterCV_NaturalKey_Active")
                .HasFilter("[IsActive] = 1");
        });


        modelBuilder.Entity<CSNDetailsEntity>(entity =>
        {
            entity.ToTable("CSNDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RateCVMasterId);
            entity.Property(e => e.MoujaId);
            entity.Property(e => e.CSN).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity.HasOne<RateMasterForCVEntity>()
                .WithMany(x => x.CSNDetails)
                .HasForeignKey(x => x.RateCVMasterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.RateCVMasterId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<GenderMasterEntity>(entity =>
        {
            entity.ToTable("GenderMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GenderName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.GenderName).IsUnique().HasDatabaseName("UQ_GenderMaster_GenderName");
            entity.HasIndex(e => e.IsActive);
        });


        // PropertyCertificateTypeMaster configuration
        modelBuilder.Entity<PropertyCertificateTypeMasterEntity>(entity =>
        {
            entity.ToTable("PropertyCertificateTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CertificateTypeName).IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasIndex(e => e.CertificateTypeName).IsUnique().HasDatabaseName("UQ_PropertyCertificateTypeMaster_Name");
        });

        modelBuilder.Entity<FloorGroupMasterEntity>(entity =>
        {
            entity.ToTable("FloorGroupMaster", "PTIS");
            entity.HasKey(e => e.Id).HasName("PK_FloorGroupMaster");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.FloorGroup).HasColumnName("FloorGroup").HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true).IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate").HasColumnType("datetime").HasDefaultValueSql("getdate()").IsRequired();
            entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
            entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate").HasColumnType("datetime");
        });


        modelBuilder.Entity<PolicyTaxDetailsCVEntity>(entity =>
        {
            entity.ToTable("PolicyTaxDetailsCV", "PTIS");
            entity.HasKey(e => e.Id).HasName("PK_PolicyTaxDetailsCV");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PolicyCode).HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.PolicyDate).HasColumnType("datetime");
            entity.Property(e => e.PolicyYear);
            entity.Property(e => e.PolicyReason);
            entity.Property(e => e.PolicyRVorCVvalue);
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.TaxAmount).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MarkedForDeletion).HasDefaultValue(false).IsRequired();
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate").HasColumnType("datetime").HasDefaultValueSql("getdate()").IsRequired();
            entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
            entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate").HasColumnType("datetime");

            // Configure foreign key relationships explicitly
            entity.HasOne(e => e.PropertyMast)
                  .WithMany(p => p.PolicyTaxDetailsCV)
                  .HasForeignKey(e => e.PropertyId)
                  .HasConstraintName("FK_PolicyTaxDetailsCV_PropertyMast_PropertyId")
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TaxMaster)
                  .WithMany(t => t.PolicyTaxDetailsCV)
                  .HasForeignKey(e => e.TaxId)
                  .HasConstraintName("FK_PolicyTaxDetailsCV_TaxMaster_TaxId")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransMastCVEntity>(entity =>
        {
            entity.ToTable("TransMastCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.FinanceYearId).IsRequired();
            entity.Property(e => e.CapitalValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            // Configure foreign key relationships explicitly
            entity.HasOne(e => e.PropertyMast)
                  .WithMany(p => p.TransMastCV)
                  .HasForeignKey(e => e.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TaxMaster)
                  .WithMany()
                  .HasForeignKey(e => e.TaxId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.YearMaster)
                  .WithMany()
                  .HasForeignKey(e => e.FinanceYearId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BlockMaster configuration
        modelBuilder.Entity<BlockMasterEntity>(entity =>
        {
            entity.ToTable("BlockMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.WardId).IsRequired();
            entity.Property(e => e.BlockNo).IsRequired().HasMaxLength(20);
            // Foreign key relationship with WardMaster
            entity.HasOne(e => e.Ward)
                 .WithMany(p => p.BlockMaster)
                 .HasForeignKey(e => e.WardId)
                 .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on WardId and BlockNo combination
            entity.HasIndex(e => new { e.WardId, e.BlockNo })
                .IsUnique()
                .HasDatabaseName("UQ_BlockMaster_Ward_BlockNo");
        });
        // TaxCategoryMaster configuration
        modelBuilder.Entity<TaxCategoryMasterEntity>(entity =>
        {
            entity.ToTable("TaxCategoryMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.CategoryCode).IsUnique().HasDatabaseName("UQ_TaxCategoryMaster_CategoryCode");
        });

        // TaxMaster configuration
        modelBuilder.Entity<TaxMasterEntity>(entity =>
        {
            entity.ToTable("TaxMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TaxCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TaxName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TaxNameAlias).HasMaxLength(200);
            entity.Property(e => e.TaxCategoryId).IsRequired();
            // Category is a [NotMapped] computed property — no column or EF configuration needed.
            entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.TaxOnUnit).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.AssessmentStatus).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.OldTaxStatus).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.TaxCode).IsUnique().HasDatabaseName("UQ_TaxMaster_TaxCode");
            entity.HasIndex(e => e.TaxName).IsUnique().HasDatabaseName("UQ_TaxMaster_TaxName");
            // FK → TaxCategoryMaster
            entity.HasOne(e => e.TaxCategoryMaster)
                  .WithMany(c => c.TaxMasters)
                  .HasForeignKey(e => e.TaxCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TransMastOld configuration
        modelBuilder.Entity<TransMastOldEntity>(entity =>
        {
            entity.ToTable("TransMastOld", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyMastOldId).IsRequired();
            entity.Property(e => e.FinanceYearId).IsRequired();
            entity.Property(e => e.RVorCV).IsRequired().HasMaxLength(2).HasColumnType("char(2)");
            entity.Property(e => e.RVorCVValue).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.TaxAmount).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Unique constraint on PropertyMastOldId, FinanceYearId, TaxId for active, non-deleted rows only
            entity.HasIndex(e => new { e.PropertyMastOldId, e.FinanceYearId, e.TaxId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [MarkedForDeletion] = 0")
                .HasDatabaseName("UQ_TransMastOld_Property_Year_Tax");

            // Performance indexes
            entity.HasIndex(e => new { e.PropertyMastOldId, e.FinanceYearId })
                .HasDatabaseName("IX_TransMastOld_PropertyYear")
                .IncludeProperties(e => new { e.TaxId, e.TaxAmount });

            entity.HasIndex(e => e.TaxId).HasDatabaseName("IX_TransMastOld_TaxId");
        });

        modelBuilder.Entity<TransMastRVEntity>(entity =>
        {
            entity.ToTable("TransMastRV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RateableValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            entity.HasOne(r => r.PropertyMast)
              .WithMany(p => p.TransMastRV)
             .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on PropertyId, FinanceYearId, TaxId for active, non-deleted rows only
            // This allows multiple historical records with the same natural key as long as only one is active
            entity.HasIndex(e => new { e.PropertyId, e.FinanceYearId, e.TaxId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [MarkedForDeletion] = 0")
                .HasDatabaseName("UQ_TransMastRV_Property_Year_Tax");

            // Performance indexes
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_TransMastRV_PropertyId");
            entity.HasIndex(e => e.FinanceYearId).HasDatabaseName("IX_TransMastRV_FinanceYearId");
            entity.HasIndex(e => e.TaxId).HasDatabaseName("IX_TransMastRV_TaxId");
        });

        // Document configuration
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Document", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            // DocumentGuid is generated application-side in DocumentEntity.Create() factory method
            // This ensures consistent behavior across all environments and improves testability
            entity.Property(e => e.DocumentGuid).IsRequired();
            entity.Property(e => e.DepartmentId);
            entity.Property(e => e.DepartmentEntityId);
            entity.Property(e => e.UploadedByUserId).IsRequired().HasColumnName("CreatedBy");

            // Ignore BaseEntity.CreatedBy since we're using UploadedByUserId mapped to CreatedBy column
            entity.Ignore(e => e.CreatedBy);

            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)");
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(200).HasColumnType("varchar(200)");
            entity.Property(e => e.FileSizeBytes).IsRequired();
            entity.Property(e => e.StorageProvider).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)").HasDefaultValue("FOLDER");
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property(e => e.ThumbnailPath).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property(e => e.ChecksumSha256).HasMaxLength(64).HasColumnType("varchar(64)");
            entity.Property(e => e.ScanStatusCode).HasMaxLength(50).HasColumnType("varchar(50)");
            entity.Property(e => e.ScanCompletedDate).HasColumnType("datetime");
            entity.Property(e => e.ScanDetails).HasMaxLength(4000).HasColumnType("nvarchar(4000)");
            entity.Property(e => e.UploadStatusCode).IsRequired().HasMaxLength(50).HasColumnType("varchar(50)").HasDefaultValue("ACTIVE");
            entity.Property(e => e.DocumentTitle).HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.Property(e => e.Description).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            entity.Property(e => e.DocumentType).HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property(e => e.DocumentCategory).HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property(e => e.Tags).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            entity.Property(e => e.Language).HasMaxLength(10).HasColumnType("varchar(10)");
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.ParentDocumentId);
            entity.Property(e => e.IsLatestVersion).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsPublic).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.InheritPermissions).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ConfidentialityLevel).HasMaxLength(50).HasColumnType("varchar(50)");
            entity.Property(e => e.PageCount);
            entity.Property(e => e.SearchableText).HasMaxLength(4000).HasColumnType("nvarchar(4000)");
            entity.Property(e => e.ExtractionStatus).HasMaxLength(50).HasColumnType("varchar(50)");
            entity.Property(e => e.EncryptionKeyId).HasMaxLength(100).HasColumnType("varchar(100)");
            entity.Property(e => e.IsEncrypted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DownloadCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.SourceSystem).HasMaxLength(100).HasColumnType("varchar(100)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // RowVersion for optimistic concurrency - database-generated timestamp
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnType("rowversion")
                .ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(e => e.DocumentGuid).IsUnique().HasDatabaseName("UQ_Document_DocumentGuid");
            entity.HasIndex(e => e.ParentDocumentId).HasDatabaseName("IX_Document_ParentDocumentId");
            entity.HasIndex(e => e.DepartmentId).HasDatabaseName("IX_Document_DepartmentId").HasFilter("[DepartmentId] IS NOT NULL");
            entity.HasIndex(e => new { e.DepartmentId, e.DepartmentEntityId }).HasDatabaseName("IX_Document_Department_Entity")
                .HasFilter("[DepartmentId] IS NOT NULL AND [DepartmentEntityId] IS NOT NULL");

            // Self-referencing FK for versioning
            entity.HasOne(d => d.ParentDocument)
                .WithMany()
                .HasForeignKey(d => d.ParentDocumentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // DocumentBinding configuration
        modelBuilder.Entity<DocumentBindingEntity>(entity =>
        {
            entity.ToTable("DocumentBinding", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.DepartmentId).IsRequired();
            entity.Property(e => e.ModuleId).IsRequired();
            entity.Property(e => e.ReferenceTableName).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
            entity.Property(e => e.ReferenceTableId);
            entity.Property(e => e.ReferenceTableIdGuid);
            entity.Property(e => e.ReferencePropertyName).IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
            entity.Property(e => e.BindingPurpose).HasMaxLength(200).HasColumnType("varchar(200)");
            entity.Property(e => e.IsPrimaryDocument).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.Notes).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property(e => e.AccessPermission).HasMaxLength(50).HasColumnType("varchar(50)");
            entity.Property(e => e.AuthDepartmentId);
            entity.Property(e => e.AuthReferenceId);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsReferenceValid).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // RowVersion for optimistic concurrency - database-generated timestamp
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnType("rowversion")
                .ValueGeneratedOnAddOrUpdate();

            entity.HasOne(db => db.Document)
                .WithMany(d => d.DocumentBindings)
                .HasForeignKey(db => db.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_DocumentBinding_DocumentId");
            entity.HasIndex(e => e.DepartmentId).HasDatabaseName("IX_DocumentBinding_DepartmentId");
            entity.HasIndex(e => e.ModuleId).HasDatabaseName("IX_DocumentBinding_ModuleId");

            entity.HasIndex(e => new { e.DepartmentId, e.ModuleId, e.ReferenceTableName, e.ReferenceTableId })
                .HasDatabaseName("IX_DocumentBinding_Reference")
                .HasFilter("[ReferenceTableId] IS NOT NULL");

            entity.HasIndex(e => new { e.DepartmentId, e.ModuleId, e.ReferenceTableName, e.ReferenceTableIdGuid })
                .HasDatabaseName("IX_DocumentBinding_ReferenceGuid")
                .HasFilter("[ReferenceTableIdGuid] IS NOT NULL");

            entity.HasIndex(e => new { e.AuthDepartmentId, e.AuthReferenceId })
                .HasDatabaseName("IX_DocumentBinding_AuthDepartment")
                .HasFilter("[AuthDepartmentId] IS NOT NULL AND [AuthReferenceId] IS NOT NULL");
        });

        // PropertyCertificate configuration
        modelBuilder.Entity<PropertyCertificateEntity>(entity =>
        {
            entity.ToTable("PropertyCertificates", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.CertificateTypeId).IsRequired();
            entity.Property(e => e.CertificateNo).HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property(e => e.IssueDate).HasColumnName("CertificateIssueDate").HasColumnType("date");
            entity.Property(e => e.DocumentBindingId);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // RowVersion for optimistic concurrency - database-generated timestamp
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnType("rowversion")
                .ValueGeneratedOnAddOrUpdate();

            entity.HasOne(pc => pc.CertificateType)
                .WithMany()
                .HasForeignKey(pc => pc.CertificateTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(pc => pc.DocumentBinding)
                .WithMany()
                .HasForeignKey(pc => pc.DocumentBindingId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_PropertyCertificates_PropertyId");
            entity.HasIndex(e => e.CertificateTypeId).HasDatabaseName("IX_PropertyCertificates_CertificateTypeId");
            entity.HasIndex(e => e.DocumentBindingId).HasDatabaseName("IX_PropertyCertificates_DocumentBindingId")
                .HasFilter("[DocumentBindingId] IS NOT NULL");
            entity.HasIndex(e => new { e.PropertyId, e.IsActive, e.MarkedForDeletion })
                .HasDatabaseName("IX_PropertyCertificates_Property_Active")
                .IncludeProperties(e => new { e.CertificateTypeId, e.CertificateNo, e.IssueDate, e.DocumentBindingId });
        });

        // PropertyPhoto configuration
        modelBuilder.Entity<PropertyPhotoEntity>(entity =>
        {
            entity.ToTable("PropertyPhoto", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PhotoTypeId).IsRequired();
            entity.Property(e => e.DocumentBindingId);
            entity.Property(e => e.IsLatest).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.DisplayOrder);
            entity.Property(e => e.Remarks).HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(p => p.PhotoType)
                .WithMany()
                .HasForeignKey(p => p.PhotoTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(p => p.DocumentBinding)
                .WithMany()
                .HasForeignKey(p => p.DocumentBindingId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_PropertyPhoto_PropertyId");
            entity.HasIndex(e => e.PhotoTypeId).HasDatabaseName("IX_PropertyPhoto_PhotoTypeId");
            entity.HasIndex(e => e.DocumentBindingId).HasDatabaseName("IX_PropertyPhoto_DocumentBindingId")
                .HasFilter("[DocumentBindingId] IS NOT NULL");

            // Non-unique helper index for the "current photos for a property" query.
            // NOTE: a property may have multiple photos per (PropertyId, PhotoTypeId), so this
            // is intentionally NOT unique. The DDL's UNIQUE index must be dropped in the DB.
            entity.HasIndex(e => new { e.PropertyId, e.PhotoTypeId })
                .HasDatabaseName("IX_PropertyPhoto_Property_Type_Latest")
                .IncludeProperties(e => new { e.DocumentBindingId, e.DisplayOrder, e.IsLatest })
                .HasFilter("[IsLatest] = 1 AND [IsActive] = 1 AND [MarkedForDeletion] = 0");
        });

        // rule scope configuration
        modelBuilder.Entity<RuleScopeEntity>(entity =>
        {
            entity.ToTable("RuleScopeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleScope).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
        });
        // Common RemarkType configuration
        modelBuilder.Entity<CommonRemarkTypeMasterEntity>(entity =>
        {
            entity.ToTable("CommonRemarkTypeMaster", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RemarkTypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.RemarkTypeName).IsUnique().HasDatabaseName("UQ_CommonRemarkTypeMaster_RemarkTypeName");
            entity.HasIndex(e => e.IsActive);
        });
        // rule effect type configuration
        modelBuilder.Entity<RuleEffectTypeEntity>(entity =>
        {
            entity.ToTable("RuleEffectTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EffectType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);

            // One-to-one relationship with EffectTypeConfiguration
            entity.HasOne(e => e.EffectTypeConfiguration)
                .WithOne(c => c.EffectType)
                .HasForeignKey<EffectTypeConfigurationEntity>(c => c.EffectTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // rule operator configuration
        modelBuilder.Entity<RuleOperatorEntity>(entity =>
        {
            entity.ToTable("RuleOperatorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Operator).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OperatorDescription).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
        });
        // RenterMast configuration
        modelBuilder.Entity<RenterMastEntity>(entity =>
        {
            entity.ToTable("RenterMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyDetailsId).IsRequired();
            entity.Property(e => e.RentMonthly).HasColumnType("float");
            entity.Property(e => e.FinalYearlyRent).HasColumnType("float");
            entity.Property(e => e.FinancialYear).HasColumnType("nvarchar(4)");
            entity.Property(e => e.DurationFrom).HasColumnType("datetime");
            entity.Property(e => e.DurationTo).HasColumnType("datetime");
            entity.Property(e => e.TaxLiability).HasColumnType("nvarchar(20)");
            entity.Property(e => e.NonCalculateRentMonthly).HasColumnType("float");
            entity.Property(e => e.RenterNameEnglish).HasColumnType("nvarchar(500)");
            entity.Property(e => e.RenterName).HasColumnType("nvarchar(500)");
            entity.Property(e => e.AgreementDate).HasColumnType("datetime");
            entity.Property(e => e.AgreementFromDate).HasColumnType("datetime");
            entity.Property(e => e.AgreementToDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Configure foreign key relationship
            entity.HasOne(e => e.PropertyDetails)
                .WithMany(p => p.Renters)
                .HasForeignKey(e => e.PropertyDetailsId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RenterMast_PropertyDetails");

            entity.HasIndex(e => e.PropertyDetailsId);
            entity.HasIndex(e => e.IsActive);
        });

        // CommonRemarkDetails configuration
        modelBuilder.Entity<CommonRemarkDetailsEntity>(entity =>
        {
            entity.ToTable("CommonRemarkDetails", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RemarkTypeId).IsRequired();
            entity.Property(e => e.Remark).IsRequired().HasMaxLength(300);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Foreign key relationship with CommonRemarkTypeMaster
            entity.HasOne<CommonRemarkTypeMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.RemarkTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.RemarkTypeId).HasDatabaseName("IX_CommonRemarkDetails_RemarkTypeId");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_CommonRemarkDetails_IsActive");
        });

        // PropertyMapMaster configuration
        modelBuilder.Entity<PropertyMapMasterEntity>(entity =>
        {
            entity.ToTable("PropertyMapMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ModuleId);
            entity.Property(e => e.ParentPropertyMapId);
            entity.Property(e => e.VersionNo).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.MappingCategory).IsRequired().HasMaxLength(30);
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Foreign key relationship with ModuleMaster
            entity.HasOne<ModuleMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PropertyMapMaster_ModuleMaster");

            // Self-referencing foreign key relationship for ParentPropertyMapId
            entity.HasOne<PropertyMapMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.ParentPropertyMapId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PropertyMapMaster_ParentPropertyMapId");

            // Indexes
            entity.HasIndex(e => e.ModuleId);
            entity.HasIndex(e => e.ParentPropertyMapId);
            entity.HasIndex(e => e.MappingCategory);
            entity.HasIndex(e => e.IsActive);
        });
        modelBuilder.Entity<RoomTypeMasterEntity>(entity =>
        {
            entity.ToTable("RoomTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoomTypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoomTypeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.RoomTypeName).IsUnique().HasDatabaseName("UQ_RoomTypeMaster_RoomTypeName");
            entity.HasIndex(e => e.RoomTypeCode).IsUnique().HasDatabaseName("UQ_RoomTypeMaster_RoomTypeCode");
            entity.HasIndex(e => e.IsActive);
        });


        // WaterConnectionTypeMaster configuration
        modelBuilder.Entity<WaterConnectionTypeEntity>(entity =>
        {
            entity.ToTable("WaterConnectionTypeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ConnectionTypeCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ConnectionTypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.ConnectionTypeCode).IsUnique().HasDatabaseName("UQ_WaterConnectionTypeMaster_Code");
            entity.HasIndex(e => e.IsActive);

            // Configure relationships
            entity.HasMany(e => e.WaterConnectionMaster)
                .WithOne(r => r.WaterConnectionType)
                .HasForeignKey(r => r.WaterConnectionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.WaterRateMaster)
                .WithOne(n => n.WaterConnectionType)
                .HasForeignKey(n => n.WaterConnectionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WaterConnectionSizeMaster configuration
        modelBuilder.Entity<WaterConnectionSizeEntity>(entity =>
        {
            entity.ToTable("WaterConnectionSizeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ConnectionSize).IsRequired().HasColumnType("decimal(5,2)");
            entity.Property(e => e.ConnectionSizeUnit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.IsActive);

            // Configure relationships
            entity.HasMany(e => e.WaterConnectionMaster)
                .WithOne(r => r.WaterConnectionSize)
                .HasForeignKey(r => r.WaterConnectionSizeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.WaterRateMaster)
                .WithOne(n => n.WaterConnectionSize)
                .HasForeignKey(n => n.WaterConnectionSizeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WaterConnectionStatusMaster configuration
        modelBuilder.Entity<WaterConnectionStatusEntity>(entity =>
        {
            entity.ToTable("WaterConnectionStatusMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.StatusName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.StatusName).IsUnique().HasDatabaseName("UQ_WaterConnectionStatusMaster_Name");
            entity.HasIndex(e => e.IsActive);

            // Configure relationships
            entity.HasMany(e => e.WaterConnectionMaster)
                .WithOne(r => r.WaterConnectionStatus)
                .HasForeignKey(r => r.WaterConnectionStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PropertyAssessmentStatusMaster configuration
        modelBuilder.Entity<PropertyAssessmentStatusEntity>(entity =>
        {
            entity.ToTable("PropertyAssessmentStatusMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.StatusName).IsRequired().HasMaxLength(30);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.StatusName).IsUnique().HasDatabaseName("UQ_PropertyAssessmentStatusMaster_StatusName");
            entity.HasIndex(e => e.IsActive);
        });

        // WaterRateMaster configuration
        modelBuilder.Entity<WaterRateMasterEntity>(entity =>
        {
            entity.ToTable("WaterRateMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.WaterConnectionTypeId).IsRequired();
            entity.Property(e => e.WaterConnectionSizeId).IsRequired();
            entity.Property(e => e.FinanceYearId).IsRequired();
            entity.Property(e => e.YearlyRate).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.WaterConnectionType)
                .WithMany(p => p.WaterRateMaster)
                .HasForeignKey(e => e.WaterConnectionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WaterConnectionSize)
                .WithMany(p => p.WaterRateMaster)
                .HasForeignKey(e => e.WaterConnectionSizeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FinanceYear)
                .WithMany()
                .HasForeignKey(e => e.FinanceYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.WaterConnectionTypeId, e.WaterConnectionSizeId, e.FinanceYearId })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UQ_WaterRateMaster_Type_Size_Year");
        });

        // WaterConnectionMaster configuration
        modelBuilder.Entity<WaterConnectionMasterEntity>(entity =>
        {
            entity.ToTable("WaterConnectionMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.WaterConnectionTypeId).IsRequired();
            entity.Property(e => e.WaterConnectionSizeId).IsRequired();
            entity.Property(e => e.ConnectionNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MeterNo).HasMaxLength(50);
            entity.Property(e => e.ConnectionStartDate).IsRequired().HasColumnType("date");
            entity.Property(e => e.ConnectionStopDate).HasColumnType("date");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(r => r.PropertyMast)
                 .WithMany(p => p.WaterConnectionMaster)
                 .HasForeignKey(r => r.PropertyId)
                 .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WaterConnectionType)
                .WithMany(p => p.WaterConnectionMaster)
                .HasForeignKey(e => e.WaterConnectionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WaterConnectionSize)
                .WithMany(p => p.WaterConnectionMaster)
                .HasForeignKey(e => e.WaterConnectionSizeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WaterConnectionStatus)
                .WithMany(p => p.WaterConnectionMaster)
                .HasForeignKey(e => e.WaterConnectionStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasMany(e => e.Details)
                .WithOne(d => d.WaterConnection)
                .HasForeignKey(d => d.WaterConnectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ConnectionNo)
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UQ_WaterConnectionMaster_ConnectionNo");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_WaterConnectionMaster_PropertyId");
            entity.HasIndex(e => e.IsActive);
        });

        // WaterConnectionDetails configuration
        modelBuilder.Entity<WaterConnectionDetailsEntity>(entity =>
        {
            entity.ToTable("WaterConnectionDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.WaterConnectionId).IsRequired();
            entity.Property(e => e.FinanceYearId).IsRequired();
            entity.Property(e => e.BillDate).IsRequired().HasColumnType("date");
            entity.Property(e => e.FromDate).IsRequired().HasColumnType("date");
            entity.Property(e => e.ToDate).IsRequired().HasColumnType("date");
            entity.Property(e => e.ChargeMonths).IsRequired();
            entity.Property(e => e.YearlyRate).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.WaterBill).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.FinanceYear)
                .WithMany()
                .HasForeignKey(e => e.FinanceYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.WaterConnection)
                .WithMany(p => p.Details)
                .HasForeignKey(e => e.WaterConnectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.WaterConnectionId, e.FinanceYearId })
                .IsUnique()
                .HasDatabaseName("UQ_WaterConnectionDetails_Connection_Year");
            entity.HasIndex(e => e.WaterConnectionId).HasDatabaseName("IX_WaterConnectionDetails_ConnectionId");
        });

        modelBuilder.Entity<BulkUpdateMasterEntity>(entity =>
        {
            entity.ToTable("BulkUpdateMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.UpdateCode).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.UpdateName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdateNameMarathi).HasMaxLength(200);
            entity.Property(e => e.IconName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ReferenceTableName).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplaySequence).HasDefaultValue(0);
            entity.Property(e => e.ApiRoute).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.UpdateCode).IsUnique().HasDatabaseName("UQ_BulkUpdateMaster_UpdateCode");
            entity.HasMany(e => e.FieldConfigs)
                .WithOne(fc => fc.Master)
                .HasForeignKey(fc => fc.BulkUpdateMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BulkUpdateFieldConfigEntity>(entity =>
        {
            entity.ToTable("BulkUpdateFieldConfig", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsReadonly).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplayNameMarathi).HasMaxLength(200);
            entity.Property(e => e.ControlType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.DataType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Placeholder).HasMaxLength(500);
            entity.Property(e => e.SequenceNo).HasDefaultValue(0);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.BindApi).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateFieldConfig_BulkUpdateMasterId");
        });

        modelBuilder.Entity<BulkUpdateHistoryEntity>(entity =>
        {
            entity.ToTable("BulkUpdateHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.BulkUpdateMasterId).IsRequired();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedColumns).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasColumnName("IPAddress").HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasOne<BulkUpdateMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.BulkUpdateMasterId)
                .HasConstraintName("FK_BulkUpdateHistory_BulkUpdateMaster")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateHistory_BulkUpdateMasterId");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_BulkUpdateHistory_PropertyId");
            // Note: IsActive, CreatedBy, CreatedDate exist in the DB table but are not yet on
            // BulkUpdateHistoryEntity — add those properties to the entity to map them here.
        });

        modelBuilder.Entity<BulkUpdateHistoryEntity>(entity =>
        {
            entity.ToTable("BulkUpdateHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.BulkUpdateMasterId).IsRequired();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedColumns).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasColumnName("IPAddress").HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateHistory_BulkUpdateMasterId");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_BulkUpdateHistory_PropertyId");
        });

        modelBuilder.Entity<BulkUpdateMasterEntity>(entity =>
        {
            entity.ToTable("BulkUpdateMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.UpdateCode).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.UpdateName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UpdateNameMarathi).HasMaxLength(200);
            entity.Property(e => e.IconName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ReferenceTableName).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplaySequence).HasDefaultValue(0);
            entity.Property(e => e.ApiRoute).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.UpdateCode).IsUnique().HasDatabaseName("UQ_BulkUpdateMaster_UpdateCode");
            entity.HasMany(e => e.FieldConfigs)
                .WithOne(fc => fc.Master)
                .HasForeignKey(fc => fc.BulkUpdateMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BulkUpdateFieldConfigEntity>(entity =>
        {
            entity.ToTable("BulkUpdateFieldConfig", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsReadonly).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.DisplayNameMarathi).HasMaxLength(200);
            entity.Property(e => e.ControlType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.DataType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Placeholder).HasMaxLength(500);
            entity.Property(e => e.SequenceNo).HasDefaultValue(0);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.BindApi).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateFieldConfig_BulkUpdateMasterId");
        });

        modelBuilder.Entity<BulkUpdateHistoryEntity>(entity =>
        {
            entity.ToTable("BulkUpdateHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IpAddress).HasColumnName("IPAddress").HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasOne<BulkUpdateMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.BulkUpdateMasterId)
                .HasConstraintName("FK_BulkUpdateHistory_BulkUpdateMaster")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateHistory_BulkUpdateMasterId");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_BulkUpdateHistory_PropertyId");
            // Note: IsActive, CreatedBy, CreatedDate exist in the DB table but are not yet on
            // BulkUpdateHistoryEntity — add those properties to the entity to map them here.
        });

        modelBuilder.Entity<BulkUpdateHistoryEntity>(entity =>
        {
            entity.ToTable("BulkUpdateHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.BulkUpdateMasterId).IsRequired();
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedColumns).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasColumnName("IPAddress").HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.BulkUpdateMasterId).HasDatabaseName("IX_BulkUpdateHistory_BulkUpdateMasterId");
            entity.HasIndex(e => e.PropertyId).HasDatabaseName("IX_BulkUpdateHistory_PropertyId");
        });

        // ApplyTaxesMaster configuration
        modelBuilder.Entity<ApplyTaxesMasterEntity>(entity =>
        {
            entity.ToTable("ApplyTaxesMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.TaxId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.ApplyTaxesMaster)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => new { e.PropertyId, e.TaxId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [MarkedForDeletion] = 0")
                .HasDatabaseName("UQ_ApplyTaxesMaster_PropertyId_TaxId");
        });

        // PropertyAssessmentDetails configuration
        modelBuilder.Entity<PropertyAssessmentDetailsEntity>(entity =>
        {
            entity.ToTable("PropertyAssessmentDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertyAssessmentDetails)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });


        // PropertyTaxCalculationSection129Results configuration
        modelBuilder.Entity<PropertyTaxCalculationSection129ResultsEntity>(entity =>
        {
            entity.ToTable("PropertyTaxCalculationSection129Results", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PropertyDetailsId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.MarkedForDeletion);
            entity.Property(e => e.MarkedForDeletionDate);

            entity.HasOne(e => e.PropertyDetails)
                .WithMany(p => p.PropertyTaxCalculationSection129Results)
                .HasForeignKey(e => e.PropertyDetailsId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertyTaxCalculationSection129Results)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyDetailsId);
            entity.HasIndex(e => e.PropertyId);
        });

        // PropertyOccupancyDetails configuration
        modelBuilder.Entity<PropertyOccupancyDetailsEntity>(entity =>
        {
            entity.ToTable("PropertyOccupancyDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyDetailId).IsRequired();
            entity.Property(e => e.OccupancyDate);
            entity.Property(e => e.OccupancyNumber).HasColumnType("nvarchar(30)");
            entity.Property(e => e.IssuedBy).HasColumnType("nvarchar(100)");
            entity.Property(e => e.Remarks).HasColumnType("nvarchar(250)");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyDetails)
                .WithMany(p => p.PropertyOccupancyDetails)
                .HasForeignKey(e => e.PropertyDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyDetailId);
        });

        // PropertyImagesMast configuration
        modelBuilder.Entity<PropertyImagesMastEntity>(entity =>
        {
            entity.ToTable("PropertyImagesMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertyImagesMast)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TaxPendingDetailsArchive configuration
        modelBuilder.Entity<TaxPendingDetailsArchiveEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetailsArchive", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TaxPendingDetailsArchive)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TaxPendingDetailsCV configuration
        modelBuilder.Entity<TaxPendingDetailsCVEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetailsCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PendingYearId).IsRequired();
            entity.Property(e => e.PendingAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TaxPendingDetailsCV)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TaxPendingDetailsLookup configuration
        modelBuilder.Entity<TaxPendingDetailsLookupEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetailsLookup", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TaxPendingDetailsLookup)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TaxPendingDetailsRetro configuration
        modelBuilder.Entity<TaxPendingDetailsRetroEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetailsRetro", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TaxPendingDetailsRetro)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TaxPendingDetailsRV configuration
        modelBuilder.Entity<TaxPendingDetailsRVEntity>(entity =>
        {
            entity.ToTable("TaxPendingDetailsRV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.PendingYearId).IsRequired();
            entity.Property(e => e.PendingAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TaxPendingDetailsRV)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TransMastArchive configuration
        modelBuilder.Entity<TransMastArchiveEntity>(entity =>
        {
            entity.ToTable("TransMastArchive", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TransMastArchive)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        // TransMastLookup configuration
        modelBuilder.Entity<TransMastLookupEntity>(entity =>
        {
            entity.ToTable("TransMastLookup", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.TransMastLookup)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
        });

        //Asset Inventory Item Category configuration
        modelBuilder.Entity<AssetCategoryEntity>(entity =>
        {
            entity.ToTable("AssetCategoryMaster", "AMS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();


            entity.Property(x => x.CategoryName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.CategoryCode).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime");
            entity.Property(x => x.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.MarkedForDeletionDate).HasColumnType("datetime");

            // Unique constraints
            entity.HasIndex(e => e.CategoryName).IsUnique().HasDatabaseName("UQ_AssetCategoryMaster_CategoryName");
            entity.HasIndex(e => e.CategoryCode).IsUnique().HasFilter("[CategoryCode] IS NOT NULL").HasDatabaseName("UQ_AssetCategoryMaster_CategoryCode");
        });

        modelBuilder.Entity<AssetTypeEntity>(entity =>
        {
            entity.ToTable("AssetTypeMaster", "AMS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd(); // Identity column

            entity.Property(x => x.TypeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.TypeName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.TypeNameLocal).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Icon).HasMaxLength(100);
            entity.Property(x => x.CodeFormat).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastSequence).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime");
            entity.Property(x => x.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(x => x.MarkedForDeletionDate).HasColumnType("datetime");

            // Unique constraints
            entity.HasIndex(e => e.TypeCode).IsUnique().HasDatabaseName("UQ_AssetTypeMaster_TypeCode");
            entity.HasIndex(e => e.TypeName).IsUnique().HasDatabaseName("UQ_AssetTypeMaster_TypeName");

            // Configure foreign key relationship to AssetCategoryEntity
            entity.HasOne<AssetCategoryEntity>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AssetTypeMaster_AssetCategoryMaster");
        });

        modelBuilder.Entity<OwningDepartmentEntity>(entity =>
        {
            entity.ToTable("OwningDepartmentMaster", "AMS");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OwningDepartmentName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.IsActive).IsRequired();

            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");

            // Unique constraint on OwningDepartmentName
            entity.HasIndex(e => e.OwningDepartmentName)
                .IsUnique()
                .HasDatabaseName("UQ_OwningDepartmentMaster_OwningDepartmentName");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
        modelBuilder.Entity<EducationTaxMasterEntity>(entity =>
       {
           entity.ToTable("EducationTaxMaster", "PTIS");
           entity.HasKey(e => e.Id);
           entity.Property(e => e.Id).ValueGeneratedOnAdd();
           entity.Property(e => e.Type).HasMaxLength(50);
           entity.Property(e => e.Year);
           entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
           entity.Property(e => e.MaxAmount).HasColumnType("decimal(18,2)");
           entity.Property(e => e.Rate).HasColumnType("decimal(18,2)");
           entity.Property(e => e.OnRVOrALV).HasMaxLength(10);
           entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
       });
        modelBuilder.Entity<EmploymentTaxMasterEntity>(entity =>
        {
            entity.ToTable("EmploymentTaxMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Year);
            entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Rate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OnRVOrALV).HasMaxLength(10);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });

        modelBuilder.Entity<OwnershipTypeEntity>(entity =>
        {
            entity.ToTable("OwnershipTypeMaster", "AMS");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OwnershipTypeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.IsActive).IsRequired();

            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            // Unique constraint on OwnershipTypeName
            entity.HasIndex(e => e.OwnershipTypeName)
                .IsUnique()
                .HasDatabaseName("UQ_OwnershipTypeMaster_OwnershipTypeName");

            entity.Property(e => e.IsActive).HasDefaultValue(true);


        });
        //Asset Inventory Item Category configuration
        modelBuilder.Entity<InventoryItemCategoryEntity>(entity =>
            {
                entity.ToTable("InventoryItemCategoryMaster", "AMS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.TypeCode).HasMaxLength(100);
                entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedBy);
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedBy);
                entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

                // Indexes for performance
                entity.HasIndex(e => e.TypeCode);
                entity.HasIndex(e => e.TypeName);
                entity.HasIndex(e => e.IsActive);
            });

        modelBuilder.Entity<InventoryItemNameEntity>(entity =>
        {
            entity.ToTable("InventoryItemNameMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InventoryItemCategoryId).IsRequired();
            entity.Property(e => e.SubTypeCode).HasMaxLength(50);
            entity.Property(e => e.SubTypeName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Indexes for performance
            entity.HasIndex(e => e.InventoryItemCategoryId);
            entity.HasIndex(e => e.SubTypeCode);
            entity.HasIndex(e => e.SubTypeName);
            entity.HasIndex(e => e.IsActive);

            // Explicit foreign key relationship
            entity.HasOne<InventoryItemCategoryEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.InventoryItemCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryItemConditionEntity>(entity =>
        {
            entity.ToTable("InventoryItemConditionMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InventoryItemCategoryId).IsRequired();
            entity.Property(e => e.ConditionName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Indexes for performance
            entity.HasIndex(e => e.InventoryItemCategoryId);
            entity.HasIndex(e => e.ConditionName);
            entity.HasIndex(e => e.IsActive);

            // Explicit foreign key relationship
            entity.HasOne<InventoryItemCategoryEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.InventoryItemCategoryId)
                  .OnDelete(DeleteBehavior.Restrict); // or .Cascade, .SetNull as per your requirement
        });

        modelBuilder.Entity<InventoryItemModelEntity>(entity =>
        {
            entity.ToTable("InventoryItemModelMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InventoryItemNameId).IsRequired();
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Indexes for performance
            entity.HasIndex(e => e.InventoryItemNameId);
            entity.HasIndex(e => e.ModelName);
            entity.HasIndex(e => e.IsActive);

            // Explicit foreign key relationship
            entity.HasOne<InventoryItemNameEntity>()
                  .WithMany()
                  .HasForeignKey(e => e.InventoryItemNameId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ScreenEntity>(entity =>
        {
            entity.ToTable("ScreenMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ScreenName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ScreenCode).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.ScreenCode).IsUnique();
            entity.Property(e => e.ScreenNameLocal).HasMaxLength(200);
            entity.Property(e => e.ScreenIcon).HasMaxLength(100);
            entity.Property(e => e.RoutePath).HasMaxLength(500);
            entity.Property(e => e.BaseRoutePath).HasMaxLength(500);
            entity.Property(e => e.RouteParamPattern).HasMaxLength(500);
            entity.Property(e => e.Purpose).HasMaxLength(100);
            entity.Property(e => e.ComponentName).HasMaxLength(200);
            entity.Property(e => e.AreaName).HasMaxLength(200);
            entity.Property(e => e.ControllerName).HasMaxLength(200);
            entity.Property(e => e.ActionName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAuthenticationRequired).HasDefaultValue(true);
            entity.Property(e => e.IsMenuVisible).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Foreign key: ParentScreenId (self-reference)
            entity.HasOne<ScreenEntity>()
                .WithMany()
                .HasForeignKey(e => e.ParentScreenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ParentScreenId);
        });

        modelBuilder.Entity<ScreenFormSectionMasterEntity>(entity =>
        {
            entity.ToTable("ScreenFormSectionMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SectionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SectionName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SectionNameLocal).HasMaxLength(200);
            entity.Property(e => e.SectionCode).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.ColumnCount).IsRequired().HasDefaultValue(2);
            entity.Property(e => e.IsOptional).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsCollapsible).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsCollapsedByDefault).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsRepeatable).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Foreign key: ScreenId
            entity.HasOne<ScreenEntity>()
                .WithMany()
                .HasForeignKey(e => e.ScreenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key: ParentSectionId (self-reference)
            entity.HasOne<ScreenFormSectionMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.ParentSectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ScreenId, e.SectionCode }).IsUnique();
            entity.HasIndex(e => e.ScreenId);
            entity.HasIndex(e => e.ParentSectionId);
        });
        modelBuilder.Entity<ScreenFormFieldMasterEntity>(entity =>
        {
            entity.ToTable("ScreenFormFieldMaster", "AMS");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FieldLabel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FieldLabelLocal).HasMaxLength(200);
            entity.Property(e => e.FieldCode).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ControlType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Placeholder).HasMaxLength(300);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.ColumnSpan).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.CssClass).HasMaxLength(200);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsReadonly).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsVisible).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsUnique).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MinValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RegexPattern).HasMaxLength(500);
            entity.Property(e => e.ValidationMessage).HasMaxLength(500);
            entity.Property(e => e.StaticOptionsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsCascading).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsMultiSelect).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.VisibilityConditionJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ValidationJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExtraConfigJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsSearchable).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsFilterable).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            // Foreign key: ScreenId
            entity.HasOne<ScreenEntity>()
                .WithMany()
                .HasForeignKey(e => e.ScreenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key: SectionId
            entity.HasOne<ScreenFormSectionMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign key: ParentFieldId (self-reference)
            entity.HasOne<ScreenFormFieldMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.ParentFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ScreenId, e.FieldCode }).IsUnique();
            entity.HasIndex(e => e.ScreenId);
            entity.HasIndex(e => e.SectionId);
            entity.HasIndex(e => e.ParentFieldId);
        });

        modelBuilder.Entity<SocialAttributeEntity>(entity =>
        {
            entity.ToTable("SocialAttributeMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SocialAttributeCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SocialAttributeName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.DisplayOrder);
            entity.Property(e => e.ParentAttributeId);
            entity.Property(e => e.IsRequiredWhenParentTrue).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsDiscountApplicable).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.PhotoTypeId);
            entity.Property(e => e.IsPhotoRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsDocumentRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });


        modelBuilder.Entity<PropertySocialDetailsEntity>(entity =>
        {
            entity.ToTable("PropertySocialDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.PropertyId).IsRequired();
            entity.Property(e => e.SocialAttributeId).IsRequired();
            entity.Property(e => e.BitValue);
            entity.Property(e => e.IntValue);
            entity.Property(e => e.DecimalValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TextValue).HasMaxLength(500);
            entity.Property(e => e.DateValue);
            entity.Property(e => e.DocumentBindingId);
            entity.Property(e => e.Remark).HasMaxLength(500);



            // Configure relationships
            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PropertySocialDetails)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SocialAttribute)
                .WithMany()
                .HasForeignKey(e => e.SocialAttributeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DocumentBinding)
                .WithMany()
                .HasForeignKey(e => e.DocumentBindingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PropertyId);
            entity.HasIndex(e => e.SocialAttributeId);
            entity.HasIndex(e => e.DocumentBindingId).HasDatabaseName("IX_PropertySocialDetails_DocumentBindingId");
            entity.HasIndex(e => new { e.PropertyId, e.SocialAttributeId }).IsUnique().HasDatabaseName("UQ_PropertySocialDetails").HasFilter("[IsActive] = 1");
        });

        // rule operator configuration
        modelBuilder.Entity<RulesFieldEntity>(entity =>
        {
            entity.ToTable("RulesFieldMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DatabaseColumnName).HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);

            // One-to-one relationship with FieldConfiguration
            entity.HasOne(e => e.FieldConfiguration)
                .WithOne(c => c.RulesField)
                .HasForeignKey<FieldConfigurationEntity>(c => c.RulesFieldId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RuleScopeFieldMapping configuration
        modelBuilder.Entity<RuleScopeFieldMappingEntity>(entity =>
        {
            entity.ToTable("RuleScopeFieldMapping", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleScopeId);
            entity.Property(e => e.RulesFieldId);
            entity.Property(e => e.DisplayOrder);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Foreign key relationships
            entity.HasOne<RuleScopeEntity>()
                .WithMany()
                .HasForeignKey(e => e.RuleScopeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<RulesFieldEntity>()
                .WithMany()
                .HasForeignKey(e => e.RulesFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => e.RuleScopeId);
            entity.HasIndex(e => e.RulesFieldId);
            entity.HasIndex(e => e.IsActive);
        });

        // FieldConfiguration configuration
        modelBuilder.Entity<FieldConfigurationEntity>(entity =>
        {
            entity.ToTable("FieldConfiguration", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            // Required fields
            entity.Property(e => e.RulesFieldId).IsRequired();
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InputType).IsRequired().HasMaxLength(50);
            // API Configuration
            entity.Property(e => e.HasApiSource).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.ApiMethod).HasMaxLength(10);
            entity.Property(e => e.ApiParameters).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ApiResponseMapping).HasColumnType("nvarchar(max)");
            // Static Value Configuration
            entity.Property(e => e.HasStaticValues).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.StaticValuesJson).HasColumnType("nvarchar(max)");
            // Validation & Default Configuration
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(255);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);
            entity.Property(e => e.MinValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinLength);
            entity.Property(e => e.MaxLength);
            // Audit Fields
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });

        // rule operator configuration
        modelBuilder.Entity<RulesFieldEntity>(entity =>
        {
            entity.ToTable("RulesFieldMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DatabaseColumnName).HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
        });

        // RuleScopeFieldMapping configuration
        modelBuilder.Entity<RuleScopeFieldMappingEntity>(entity =>
        {
            entity.ToTable("RuleScopeFieldMapping", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleScopeId);
            entity.Property(e => e.RulesFieldId);
            entity.Property(e => e.DisplayOrder);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Foreign key relationships
            entity.HasOne<RuleScopeEntity>()
                .WithMany()
                .HasForeignKey(e => e.RuleScopeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<RulesFieldEntity>()
                .WithMany()
                .HasForeignKey(e => e.RulesFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => e.RuleScopeId);
            entity.HasIndex(e => e.RulesFieldId);
            entity.HasIndex(e => e.IsActive);
        });

        // FieldConfiguration configuration
        modelBuilder.Entity<FieldConfigurationEntity>(entity =>
        {
            entity.ToTable("FieldConfiguration", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            // Required fields
            entity.Property(e => e.RulesFieldId).IsRequired();
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InputType).IsRequired().HasMaxLength(50);
            // API Configuration
            entity.Property(e => e.HasApiSource).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.ApiMethod).HasMaxLength(10);
            entity.Property(e => e.ApiParameters).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ApiResponseMapping).HasColumnType("nvarchar(max)");
            // Static Value Configuration
            entity.Property(e => e.HasStaticValues).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.StaticValuesJson).HasColumnType("nvarchar(max)");
            // Validation & Default Configuration
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(255);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);
            entity.Property(e => e.MinValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinLength);
            entity.Property(e => e.MaxLength);
            // Audit Fields
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Foreign key relationship — one-to-one: one RulesField has one FieldConfiguration
            entity.HasOne(e => e.RulesField)
                .WithOne(r => r.FieldConfiguration)
                .HasForeignKey<FieldConfigurationEntity>(e => e.RulesFieldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => e.IsActive);
        });

        // EffectTypeConfiguration configuration
        modelBuilder.Entity<EffectTypeConfigurationEntity>(entity =>
        {
            entity.ToTable("EffectTypeConfiguration", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            // Required fields
            entity.Property(e => e.EffectTypeId).IsRequired();
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InputType).IsRequired().HasMaxLength(50);
            // API Configuration
            entity.Property(e => e.HasApiSource).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.ApiMethod).HasMaxLength(10);
            entity.Property(e => e.ApiParameters).HasColumnType("nvarchar(max)");

            // Static API Configuration
            entity.Property(e => e.StaticApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.StaticApiInputType).HasMaxLength(500);
            entity.Property(e => e.StaticApiMethod).HasMaxLength(500);
            entity.Property(e => e.StaticApiParamter).HasMaxLength(500);
            entity.Property(e => e.StaticApiResponseMapping).HasMaxLength(500);
            // Static Value Configuration
            entity.Property(e => e.HasStaticValues).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.StaticValuesJson).HasColumnType("nvarchar(max)");
            // Validation & Default Configuration
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(255);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);
            entity.Property(e => e.MinValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinLength);
            entity.Property(e => e.MaxLength);
            entity.Property(e => e.ExpressionTemplate).HasMaxLength(500);
            // Audit Fields
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Unique constraint on EffectTypeId
            entity.HasIndex(e => e.EffectTypeId).IsUnique().HasDatabaseName("UQ_EffectTypeConfiguration_EffectTypeId");
            // Indexes for performance
            entity.HasIndex(e => e.IsActive);
        });

        // RuleEngineMaster configuration
        modelBuilder.Entity<RuleEngineEntity>(entity =>
        {
            entity.ToTable("RuleEngineMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Required fields
            entity.Property(e => e.RuleCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RuleCategory).HasMaxLength(100);
            entity.Property(e => e.RuleJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ConditionsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.EffectJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TargetFiltersJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Priority).IsRequired().HasDefaultValue(100);

            entity.Property(e => e.IsEnabled).IsRequired().HasDefaultValue(true);

            // Audit fields
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // New property for stop processing
            entity.Property(e => e.StopProcessing).IsRequired().HasDefaultValue(false);

            // IHardDeletable
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);

            // Unique constraint on RuleCode
            entity.HasIndex(e => e.RuleCode).IsUnique().HasDatabaseName("UQ_RuleEngineMaster_RuleCode");

            // Indexes for performance
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.IsActive);
        });

        // RuleCategoryMaster configuration
        modelBuilder.Entity<RuleCategoryEntity>(entity =>
        {
            entity.ToTable("RuleCategoryMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(0);

            // Audit fields
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Unique constraint on CategoryCode
            entity.HasIndex(e => e.CategoryCode).IsUnique().HasDatabaseName("UQ_RuleCategoryMaster_CategoryCode");

            // Indexes for performance
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
        });

        // RuleVersionHistory configuration
        modelBuilder.Entity<RuleVersionHistoryEntity>(entity =>
        {
            entity.ToTable("RuleVersionHistory", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Required fields
            entity.Property(e => e.RuleId).IsRequired();
            entity.Property(e => e.RuleCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RuleJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();

            // Change metadata
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.ChangedBy).IsRequired();
            entity.Property(e => e.ChangedDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ChangeSummary).HasColumnType("nvarchar(max)");

            // Foreign key relationship
            entity.HasOne(e => e.RuleEngine)
                .WithMany()
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.RuleId);
            entity.HasIndex(e => e.RuleCode);
            entity.HasIndex(e => e.Version);
            entity.HasIndex(e => e.ChangeType);
            entity.HasIndex(e => e.ChangedDate);
        });

        // TypeOfUseGroupMasterCV configuration
        modelBuilder.Entity<TypeOfUseGroupCVEntity>(entity =>
        {
            entity.ToTable("TypeOfUseGroupMasterCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.TypeOfUseGroupCVCode).IsRequired().HasMaxLength(10).HasColumnType("varchar(10)");
            entity.Property(e => e.GroupName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.GroupIcon).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsFloorWiseRateApplicable);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            // Unique constraints
            entity.HasIndex(e => e.TypeOfUseGroupCVCode).IsUnique().HasDatabaseName("UQ_TypeOfUseGroupMasterCV_TypeOfUseGroupCVCode");
            entity.HasIndex(e => e.GroupName).IsUnique().HasDatabaseName("UQ_TypeOfUseGroupMasterCV_GroupName");
            entity.HasIndex(e => e.IsActive);


            entity.HasMany(e => e.TypeOfUse)
           .WithOne(n => n.TypeOfUseGroupCV)
           .HasForeignKey(n => n.TypeOfUseGroupCVId)
           .OnDelete(DeleteBehavior.Restrict);
        });

        // AssetDocumentDefinition configuration
        modelBuilder.Entity<AssetDocumentDefinitionEntity>(entity =>
        {
            entity.ToTable("AssetDocumentDefinition", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AssetCategoryId).IsRequired();
            entity.Property(e => e.AssetTypeId).IsRequired(false);
            entity.Property(e => e.DocumentCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MaxFileSizeMB).IsRequired().HasDefaultValue(10);
            entity.Property(e => e.AllowedExtensions).IsRequired().HasMaxLength(200).HasDefaultValue(".pdf,.jpg,.jpeg,.png,.doc,.docx");
            entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");

            entity.HasOne<AssetCategoryEntity>()
                .WithMany()
                .HasForeignKey(e => e.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AssetTypeEntity>()
                .WithMany()
                .HasForeignKey(e => e.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.AssetCategoryId, e.AssetTypeId, e.DocumentCode })
                .IsUnique()
                .HasDatabaseName("UQ_DocDef_CategoryTypeCode")
                .HasFilter("[AssetTypeId] IS NOT NULL");

            entity.HasIndex(e => new { e.AssetCategoryId, e.DocumentCode })
                .IsUnique()
                .HasDatabaseName("UQ_DocDef_CategoryCode_WhenTypeNull")
                .HasFilter("[AssetTypeId] IS NULL");
        });

        // AssetFieldDefinition configuration
        modelBuilder.Entity<AssetFieldDefinitionEntity>(entity =>
        {
            entity.ToTable("AssetFieldDefinition", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AssetCategoryId).IsRequired();
            entity.Property(e => e.AssetTypeId).IsRequired();
            entity.Property(e => e.FieldCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldLabel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FieldGroup).HasMaxLength(100);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DisplayOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.ValidationRules).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.MinValue).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MaxValue).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MaxLength);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");

            entity.HasOne<AssetCategoryEntity>()
                .WithMany()
                .HasForeignKey(e => e.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AssetTypeEntity>()
                .WithMany()
                .HasForeignKey(e => e.AssetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.AssetCategoryId, e.AssetTypeId, e.FieldCode })
                .IsUnique()
                .HasDatabaseName("UQ_FieldDef_CategoryTypeField");
        });

        // AssetAuthorityMaster configuration
        modelBuilder.Entity<AssetAuthorityMasterEntity>(entity =>
        {
            entity.ToTable("AuthorityMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AuthorityCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AuthorityName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");

            entity.HasIndex(e => e.AuthorityCode)
                .IsUnique()
                .HasDatabaseName("UQ_AuthorityMaster_AuthorityCode");
        });

        // AssetOrganizationMaster configuration
        modelBuilder.Entity<AssetOrganizationMasterEntity>(entity =>
        {
            entity.ToTable("OrganizationMaster", "AMS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AuthorityId).IsRequired();
            entity.Property(e => e.OrganizationCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime");

            entity.HasOne<AssetAuthorityMasterEntity>()
                .WithMany()
                .HasForeignKey(e => e.AuthorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrganizationCode)
                .IsUnique()
                .HasDatabaseName("UQ_OrganizationMaster_OrgCode");
        });
    }
}
