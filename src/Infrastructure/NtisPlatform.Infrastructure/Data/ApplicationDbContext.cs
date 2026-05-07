using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

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
    public DbSet<TaxZoneEntity> TaxZoneMaster { get; set; } = null!;
    public DbSet<AssessmentYearRangeEntity> AssessmentYearRangeEntities { get; set; } = null!;
    public DbSet<RetentionFactWiseEntity> RetentionFactWiseEntities { get; set; } = null!;
    public DbSet<UserRoleMasterEntity> UserRoleMasterEntity { get; set; } = null!;
    public DbSet<MoujaEntity> MoujaEntity { get; set; } = null!;
    public DbSet<OfficeEntity> OfficeEntity { get; set; } = null!;
    public DbSet<RetentionYearWiseEntity> RetentionYearWiseEntities { get; set; } = null!;
    public DbSet<SubTypeOfUseEntity> SubTypeOfUse { get; set; } = null!;
    public DbSet<TypeOfUseEntity> TypeOfUse { get; set; } = null!;
    public DbSet<RuleEntity> RuleMaster { get; set; } = null!;
    public DbSet<AssessmentYearRangeCVEntity> AssessmentYearRangeCVEntities { get; set; } = null!;
    public DbSet<TypeOfUseGroupEntity> TypeOfUseGroup { get; set; } = null!;
    public DbSet<DepreciationMasterEntity> DepreciationMaster { get; set; } = null!;
    public DbSet<FloorFactorCVMasterEntity> FloorFactorCVMasters { get; set; } = null!;
    public DbSet<ZoneEntity> ZoneMaster { get; set; } = null!;
    public DbSet<WardEntity> WardMaster { get; set; } = null!;
    public DbSet<BankMasterEntity> BankMasters { get; set; } = null!;
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
    public DbSet<PolicyTaxDetailsEntity> PolicyTaxDetails { get; set; } = null!;
    public DbSet<PolicyTaxDetailsCVEntity> PolicyTaxDetailsCV { get; set; } = null!;
    public DbSet<TransMastOldEntity> TransMastOld { get; set; } = null!;
    public DbSet<TaxMasterEntity> TaxMaster { get; set; } = null!;
    public DbSet<PropertyTypeCategoryEntity> PropertyTypeCategoryMaster { get; set; } = null!;
    public DbSet<PropertyTypeMasterEntity> PropertyTypeMasters { get; set; } = null!;
    public DbSet<ConfigValueMasterEntity> ConfigValueMasters { get; set; } = null!;
    public DbSet<TransMastCVEntity> TransMastCV { get; set; } = null!;
    public DbSet<UserEntity> UserMasters { get; set; } = null!;
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;
    public DbSet<UserDepartmentAllocationEntity> UserDepartmentAllocation { get; set; } = null!;
    public DbSet<UserModuleAllocationEntity> UserModuleAllocation { get; set; } = null!;
    public DbSet<UserRoleAllocationEntity> UserRoleAllocation { get; set; } = null!;
    public DbSet<EmployeeTypeEntity> EmployeeType { get; set; } = null!;
    public DbSet<PropertyDescriptionAndTypeOfUseValidationEntity> PropertyDescriptionAndTypeOfUseValidations { get; set; } = null!;
    public DbSet<GenderMasterEntity> GenderMasters { get; set; } = null!;
    public DbSet<PropertyCertificateTypeMasterEntity> PropertyCertificateTypeMasters { get; set; } = null!;
    public DbSet<AgeFactorCVMasterEntity> AgeFactorCVMasters { get; set; } = null!;
    public DbSet<NatureFactorCVMasterEntity> NatureFactorCVMasters { get; set; } = null!;
    public DbSet<TaxPercentageMasterRV> TaxPercentageMasterRVs { get; set; } = null!;
    public DbSet<TaxPercentageMasterCV> TaxPercentageMasterCVs { get; set; } = null!;
    public DbSet<UseFactorCVMasterEntity> UseFactorCVMaster { get; set; } = null!;
    public DbSet<BlockMasterEntity> BlockMaster { get; set; } = null!;
    public DbSet<ParkingTypeMasterEntity> ParkingTypeMaster { get; set; } = null!;
    public DbSet<RuleScopeEntity> RuleScope { get; set; } = null!;
    public DbSet<CommonRemarkTypeMasterEntity> CommonRemarkTypeMasters { get; set; } = null!;
    public DbSet<RuleEffectTypeEntity> RuleEffectTypeMaster { get; set; } = null!;
    public DbSet<RenterMastEntity> RenterMast { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PolicyTaxDetailsEntity>(entity =>
        {
            entity.ToTable("PolicyTaxDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            // Configure foreign key relationships
            entity.HasOne(e => e.TaxMaster)
                .WithMany()
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PolicyTaxDetails)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyTaxDetailsCVEntity>(entity =>
        {
            entity.ToTable("PolicyTaxDetailsCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate).HasColumnType("datetime").IsRequired(false);

            // Configure foreign key relationships
            entity.HasOne(e => e.TaxMaster)
                .WithMany()
                .HasForeignKey(e => e.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PropertyMast)
                .WithMany(p => p.PolicyTaxDetailsCV)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // AgeFactorCVMaster configuration
        modelBuilder.Entity<AgeFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("AgeFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.ConstructionTypeId).IsRequired();
            entity.Property(e => e.AgeFrom).IsRequired();
            entity.Property(e => e.AgeTo).IsRequired();
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
            // Uniqueness constraint for ConstructionTypeId + AgeFrom + AgeTo + YearRangeCVId
            entity.HasIndex(e => new { e.ConstructionTypeId, e.AgeFrom, e.AgeTo, e.YearRangeCVId }).IsUnique();
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
        });

        // UseFactorCVMaster configuration
        modelBuilder.Entity<UseFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("UseFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.TypeOfUseId).IsRequired();
            entity.Property(e => e.SubTypeOfUseId).IsRequired();
            entity.Property(e => e.Factor)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.TypeOfUseId);
            entity.HasIndex(e => e.SubTypeOfUseId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.IsActive);
            // Uniqueness constraint for TypeOfUseId + SubTypeOfUseId + YearRangeCVId
            entity.HasIndex(e => new { e.TypeOfUseId, e.SubTypeOfUseId, e.YearRangeCVId }).IsUnique();
        });

        // BlockMaster configuration
        modelBuilder.Entity<BlockMasterEntity>(entity =>
        {
            entity.ToTable("BlockMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Identity column
            entity.Property(e => e.WardId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.WardId);
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
        });

        modelBuilder.Entity<FloorEntity>(entity =>
        {
            entity.ToTable("FloorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FloorCode);
            entity.Property(e => e.Description);
            entity.Property(e => e.SequenceNo);
            entity.Property(e => e.MaxFloorNo);
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
            entity.Property(e => e.RateSquareMeter);
            entity.Property(e => e.RateSquareFeet);
            entity.Property(e => e.Id);
            entity.Property(e => e.RateRemark);
            entity.Property(e => e.IsActive);
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
        });
        modelBuilder.Entity<AssessmentYearRangeCVEntity>(entity =>
        {
            entity.ToTable("AssessmentYearRangeMasterCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromYear);
            entity.Property(e => e.ToYear);
            entity.Property(e => e.IsActive);
            // Unique constraint for FromYear-ToYear pair
            entity.HasIndex(e => new { e.FromYear, e.ToYear }).IsUnique();
        });
        modelBuilder.Entity<SubFloorEntity>(entity =>
        {
            entity.ToTable("SubFloorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubFloorCode);
            entity.Property(e => e.Description);
            entity.Property(e => e.SubFloorPercentage);

        });
        modelBuilder.Entity<WardEntity>(entity =>
        {
            entity.ToTable("WardMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.WardNo).IsRequired().HasMaxLength(10);
            entity.Property(x => x.Id).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(50);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(x => x.WardNo).IsUnique();
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
        });
        modelBuilder.Entity<TypeOfUseEntity>(entity =>
        {
            entity.ToTable("TypeOfUseMaster", "PTIS");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TypeOfUseCode);
            entity.Property(x => x.Description);
            entity.Property(x => x.Type);
            entity.Property(x => x.TypeOfUseGroupId);
            entity.Property(x => x.SearchSequence);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedDate);
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
            entity.ToTable("RateMasterForCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OpenPlotRate).HasColumnType("money");
            entity.Property(e => e.ResidentialRate).HasColumnType("money");
            entity.Property(e => e.OfficeRate).HasColumnType("money");
            entity.Property(e => e.ShopRate).HasColumnType("money");
            entity.Property(e => e.IndustrialRate).HasColumnType("money");
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
        });

        // TaxPercentageMasterRV configuration
        modelBuilder.Entity<TaxPercentageMasterRV>(entity =>
        {
            entity.ToTable("TaxPercentageMasterRV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.YearRangeRVId).IsRequired();
            entity.Property(e => e.TypeOfUseId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.YearRangeRVId);
            entity.HasIndex(e => e.TypeOfUseId);

        });
        // TaxPercentageMasterCV configuration
        modelBuilder.Entity<TaxPercentageMasterCV>(entity =>
        {
            entity.ToTable("TaxPercentageMasterCV", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.TypeOfUseId).IsRequired();
            // Add other property configurations as needed
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.TypeOfUseId);

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
            entity.Property(e => e.Year);
            entity.Property(e => e.MoujaName).IsRequired().HasMaxLength(50);
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
        modelBuilder.Entity<RuleEntity>(entity =>
        {
            entity.ToTable("RuleMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DefaultValue).HasMaxLength(50);
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
            entity.Property(x => x.RateSectionNo);
            entity.Property(x => x.Description);

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
        });
        modelBuilder.Entity<ScreenMasterEntity>(entity =>
        {
            entity.ToTable("ScreenMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ScreenGroupId).HasColumnName("ScreenGroupId");
            entity.Property(e => e.ModuleId).HasColumnName("ModuleId");
            entity.HasOne(e => e.ScreenGroup)
                .WithMany()
                .HasForeignKey(e => e.ScreenGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Module)
                .WithMany()
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

        });


        modelBuilder.Entity<ScreenGroupMasterEntity>(entity =>
        {
            entity.ToTable("ScreenGroupMaster", "Core");
            entity.HasKey(e => e.Id);
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
                .HasForeignKey(e => e.DepartmentMasterId)
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
                .HasColumnName("DesignationId");
            entity.Property(e => e.DesignationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DesignationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DesignationLocal).HasMaxLength(200);
            entity.Property(e => e.DesignationDescription).HasMaxLength(500);
            // Indexes
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.DesignationCode).IsUnique();
        });

        // FloorFactorCVMaster configuration
        modelBuilder.Entity<FloorFactorCVMasterEntity>(entity =>
        {
            entity.ToTable("FloorFactorCVMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FloorId).IsRequired();
            entity.Property(e => e.FactorWithLift)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.FactorWithoutLift)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearRangeCVId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.FloorId);
            entity.HasIndex(e => e.YearRangeCVId);
            entity.HasIndex(e => e.IsActive);
            // Uniqueness constraint for FloorId + YearRangeCVId
            entity.HasIndex(e => new { e.FloorId, e.YearRangeCVId }).IsUnique();
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
            entity.Property(e => e.PropertyTypeGroup).HasMaxLength(50);
            entity.Property(e => e.SearchSequence);
            entity.Property(e => e.PropertyTypeCategoryId);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyDescription).IsUnique().HasDatabaseName("UQ_PropertyTypeMaster_PropertyDescription");
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
            entity.Property(e => e.AssessmentNo).HasMaxLength(10);
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
            entity.HasIndex(e => e.PropertyId);

            // Ignore columns that don't exist in database
            // PropertyMastDetails table schema:
            // ? MarkedForDeletion column EXISTS (mapped above)
            // ? MarkedForDeletionDate column DOES NOT EXIST in database yet
            // Entity has MarkedForDeletionDate property for IHardDeletable support,
            // but we ignore it in EF Core to prevent SQL errors until column is added to database
            entity.Ignore(e => e.MarkedForDeletionDate);

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
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.Id);
        });

        // PlotDetails configuration
        modelBuilder.Entity<PlotDetailsEntity>(entity =>
        {
            entity.ToTable("PlotDetails", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.PlotArea).HasColumnType("float");
            entity.Property(e => e.PlotAreaFtLength).HasColumnType("float");
            entity.Property(e => e.PlotAreaFtWidth).HasColumnType("float");
            entity.Property(e => e.PlotAreaMtrLength).HasColumnType("float");
            entity.Property(e => e.PlotAreaMtrWidth).HasColumnType("float");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.Id);
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
            entity.Property(e => e.SecretaryMobileNo).HasMaxLength(13);
            entity.Property(e => e.SocietyEmailId).HasMaxLength(100);
            entity.Property(e => e.SecretaryEmailId).HasMaxLength(100);
            entity.Property(e => e.ManagerEmailId).HasMaxLength(100);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            //entity.Ignore(e => e.BHKType);
            // SocietyDetailsMast table schema:
            // ? MarkedForDeletion column EXISTS (mapped above)
            // ? MarkedForDeletionDate column DOES NOT EXIST in database yet
            // Entity has MarkedForDeletionDate property for IHardDeletable support,
            // but we ignore it in EF Core to prevent SQL errors until column is added to database
            entity.Ignore(e => e.MarkedForDeletionDate);
        });

        // Property configuration
        modelBuilder.Entity<PropertyEntity>(entity =>
        {
            entity.ToTable("PropertyMast", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.PropertyNo).HasMaxLength(10);
            entity.Property(e => e.PartitionNo).HasMaxLength(10);
            entity.Property(e => e.Id);
            entity.Property(e => e.UPICId).HasMaxLength(30);
            entity.Property(e => e.OpenPlot);
            entity.Property(e => e.CSN).HasMaxLength(30);
            entity.Property(e => e.SubZoneNo).HasMaxLength(20);
            entity.Property(e => e.PlotNo).HasMaxLength(20);
            entity.Property(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(5);
            entity.Property(e => e.PartType).HasMaxLength(20);
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
            entity.Property(e => e.Id);
            entity.Property(e => e.OwnerTitleEnglish).HasMaxLength(20);
            entity.Property(e => e.OwnerNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.OccupierTitleEnglish).HasMaxLength(20);
            entity.Property(e => e.OccupierNameEnglish).HasMaxLength(1000);
            entity.Property(e => e.FlatOrShopNoEnglish).HasMaxLength(100);
            entity.Property(e => e.FlatOrShopNameEnglish).HasMaxLength(200);
            entity.Property(e => e.AddressEnglish).HasMaxLength(500);
            entity.Property(e => e.LocationEnglish).HasMaxLength(200);
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);

            // Ignore MarkedForDeletionDate as column doesn't exist in database yet
            // PropertyMast table schema:
            // ? MarkedForDeletion column EXISTS (mapped above)
            // ? MarkedForDeletionDate column DOES NOT EXIST in database yet
            // Entity has MarkedForDeletionDate property for IHardDeletable support,
            // but we ignore it in EF Core to prevent SQL errors until column is added to database
            entity.Ignore(e => e.MarkedForDeletionDate);

            // Unique index on WardId, PropertyNo, PartitionNo
            entity.HasIndex(e => new { e.Id, e.PropertyNo, e.PartitionNo })
                .IsUnique()
                .HasFilter("[PropertyNo] IS NOT NULL AND [PartitionNo] IS NOT NULL")
                .HasDatabaseName("UQ_Property_Ward_Property_Partition");
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
                .HasForeignKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.ConfigCode).IsUnique();
            entity.HasIndex(e => e.Id);
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
            entity.Property(e => e.ReplacedByTokenId);
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
            entity.Property(e => e.Id).ValueGeneratedOnAdd();  // Database auto-generates Id (IDENTITY)
            entity.Property(e => e.PropertyMastOldId).IsRequired();
            entity.Property(e => e.OldFloorId);  // int? FK to FloorMaster.Id
            entity.Property(e => e.OldSubFloorId);  // int? FK to SubFloorMaster.Id
            entity.Property(e => e.OldConstructionYear).HasMaxLength(4);  // varchar(4) - year string
            entity.Property(e => e.OldAssessmentYear).HasMaxLength(4);  // nvarchar(4) - year string
            entity.Property(e => e.OldConstructionTypeId);  // int? FK to ConstructionTypeMaster.Id
            entity.Property(e => e.OldTypeOfUseId);  // int? FK to TypeOfUseMaster.Id
            entity.Property(e => e.OldSubTypeOfUseId);  // int? FK to SubTypeOfUseMaster.Id
            entity.Property(e => e.OldCarpetAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.OldCarpetAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.OldBuiltupAreaSqMeter).HasColumnType("float");
            entity.Property(e => e.OldBuiltupAreaSqFeet).HasColumnType("float");
            entity.Property(e => e.MarkedForDeletion).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.MarkedForDeletionDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.HasIndex(e => e.PropertyMastOldId);
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
        // PropertyDescriptionAndTypeOfUseValidation configuration
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

            // Unique constraint on PropertyTypeId and TypeOfUseId combination
            entity.HasIndex(e => new { e.PropertyTypeId, e.TypeOfUseId })
                .IsUnique()
                .HasDatabaseName("UQ_PropertyDescriptionAndTypeOfUseValidation_PropertyTypeId_TypeOfUseId");
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
            entity.Property(e => e.CertificateTypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CertificateTypeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FieldCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SectionCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DocumentTypeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayLabel).HasMaxLength(200);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IsMandatory).HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
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
        // rule scope configuration
        modelBuilder.Entity<RuleScopeEntity>(entity =>
        {
            entity.ToTable("RuleScopeMaster", "CORE");
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
            entity.ToTable("RuleEffectTypeMaster", "CORE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EffectType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
        });

        // rule operator configuration
        modelBuilder.Entity<RuleOperatorEntity>(entity =>
        {
            entity.ToTable("RuleOperatorMaster", "CORE");
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

        
    }
}
