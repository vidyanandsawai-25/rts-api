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

    // Authentication entities
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;

    // Organization configuration
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<OrganizationSetting> OrganizationSettings { get; set; } = null!;
    public DbSet<AuthProvider> AuthProviders { get; set; } = null!;
    public DbSet<FeatureFlag> FeatureFlags { get; set; } = null!;
    public DbSet<ConstructionTypeEntity> ConstructionTypeEntity { get; set; } = null!;
    public DbSet<FloorEntity> FloorEntity { get; set; } = null!;
    public DbSet<SubFloorEntity> SubFloorEntity { get; set; } = null!;
    public DbSet<RateEntity> RateEntity { get; set; } = null!;
    public DbSet<MultilingualDetailsEntity> MultilingualDetails { get; set; } = null!;
    public DbSet<RateMasterForCVEntity> RateMasterForCVs { get; set; } = null!;
    public DbSet<TaxZoneEntity> TaxZoneEntity { get; set; } = null!;
    public DbSet<RetentionFactWiseEntity> RetentionFactWiseEntities { get; set; } = null!;
    public DbSet<UserRoleMasterEntity> UserRoleMasterEntity { get; set; } = null!;
    public DbSet<MoujaEntity> MoujaEntity { get; set; } = null!;
    public DbSet<OfficeEntity> OfficeEntity { get; set; } = null!;
    public DbSet<RetentionYearWiseEntity> RetentionYearWiseEntities { get; set; } = null!;
    public DbSet<SubTypeOfUseEntity> SubTypeOfUse { get; set; } = null!;
    public DbSet<TypeOfUseEntity> TypeOfUse { get; set; } = null!;
    public DbSet<TypeOfUseGroupEntity> TypeOfUseGroup { get; set; } = null!;
    public DbSet<DepreciationMasterEntity> DepreciationMaster { get; set; } = null!;
    public DbSet<ZoneEntity> Zones { get; set; } = null!;
    public DbSet<WardEntity> WardEntity { get; set; } = null!;
    public DbSet<BankMasterEntity> BankMasters { get; set; } = null!;
    public DbSet<YearMasterEntity> YearMaster { get; set; } = null!;
    public DbSet<ScreenMasterEntity> ScreenMaster { get; set; } = null!;
    public DbSet<ScreenGroupMasterEntity> ScreenGroupMaster { get; set; } = null!;
    public DbSet<RateSectionEntity> RateSection { get; set; } = null!;   
    public DbSet<ModuleMasterEntity> ModuleMasters { get; set; } = null!;
    public DbSet<ActiveTaxesEntity> ActiveTaxesMasters { get; set; } = null!;
    public DbSet<DepartmentLicenceDetailsEntity> DepartmentLicenceDetails { get; set; } = null!;
    public DbSet<RateSectionDetailsEntity> RateSectionDetails { get; set; } = null!;
    public DbSet<ScreenGroupMasterEntity> ScreenGroupMasters { get; set; } = null!;
    public DbSet<YearMasterEntity> YearMasterEntity { get; set; } = null!;
    public DbSet<DesignationMasterEntity> DesignationMasters { get; set; } = null!;
    public DbSet<DepartmentMasterEntity> DepartmentMasters { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ConstructionTypeEntity>(entity =>
        {
            entity.ToTable("ConstructionTypeMaster", "PTIS");
            entity.HasKey(e => e.ConstructionId);
            entity.Property(e => e.Description);
            entity.Property(e => e.DescriptionEnglish);
            entity.Property(e => e.KeyboardShortCutKey);
            entity.Property(e => e.KeyWiseSequence);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
        });

        modelBuilder.Entity<FloorEntity>(entity =>
        {
            entity.ToTable("FloorMaster", "PTIS");
            entity.HasKey(e => e.FloorID);
            entity.Property(e => e.Description);
            entity.Property(e => e.SequenceNo);
            entity.Property(e => e.DescriptionEnglish);
            entity.Property(e => e.MaxFloorNo);
        });

        modelBuilder.Entity<RateEntity>(entity =>
        {
            entity.ToTable("RateMaster", "PTIS");
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Year);
            entity.Property(e => e.TaxZoneNo);
            entity.Property(e => e.FloorID);
            entity.Property(e => e.TypeOfUseGroupID);
            entity.Property(e => e.ConstructionID);
            entity.Property(e => e.MinYear);
            entity.Property(e => e.MaxYear);
            entity.Property(e => e.RateSquareMeter);
            entity.Property(e => e.RateSquareFeet);
            entity.Property(e => e.RateSectionNo);
            entity.Property(e => e.RateRemark);
            entity.Property(e => e.IsActive);
        });

        modelBuilder.Entity<RetentionFactWiseEntity>(entity =>
        {
            entity.ToTable("RetentionPolicyFactWiseMaster", "PTIS");
            entity.HasKey(e => e.ID);
            entity.Property(e => e.FromFactor);
            entity.Property(e => e.ToFactor);
            entity.Property(e => e.FactorValue);
            entity.Property(e => e.IsActive);
        });

        modelBuilder.Entity<RetentionYearWiseEntity>(entity =>
        {
            entity.ToTable("RetentionPolicyYearWiseMaster", "PTIS");
            entity.HasKey(e => e.ID);
            entity.Property(e => e.FromYear);
            entity.Property(e => e.ToYear);
            entity.Property(e => e.FactorValue);
            entity.Property(e => e.IsActive);
        });
        modelBuilder.Entity<SubFloorEntity>(entity =>
        {
            entity.ToTable("SubFloorMaster", "PTIS");
            entity.HasKey(e => e.SubFloorId);
            entity.Property(e => e.SubFloorDescription);
            entity.Property(e => e.SubFloorDescriptionEnglish);
            entity.Property(e => e.SubFloorPercentage);

        });
        modelBuilder.Entity<WardEntity>(entity =>
        {
            entity.ToTable("WardMaster", "PTIS");
            entity.HasKey(x => x.WardNo);
            entity.Property(x => x.WardNo);
            entity.Property(x => x.ZoneNo);
            entity.Property(x => x.Description);
            entity.Property(x => x.DescriptionEnglish);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive);
        });

        modelBuilder.Entity<SubTypeOfUseEntity>(entity =>
        {
            entity.ToTable("SubTypeOfUseMaster", "PTIS");
            entity.HasKey(x => x.SubTypeOfUseId);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(80);
            entity.Property(x => x.DescriptionEnglish).HasMaxLength(80);
            entity.Property(x => x.TypeOfUseID).IsRequired().HasMaxLength(50);
            entity.Property(x => x.SearchKey).HasMaxLength(20);
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
            entity.HasKey(x => x.TypeOfUseID);
            entity.Property(x => x.TypeOfUseID).HasMaxLength(10);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(80);
            entity.Property(x => x.DescriptionEnglish).HasMaxLength(80);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(5);
            entity.Property(x => x.GroupID).IsRequired().HasMaxLength(50);
            entity.Property(x => x.SearchKey).HasMaxLength(20);
            entity.Property(x => x.SearchSequence);
            entity.Property(x => x.IsActive);
            entity.Property(x => x.IsSociety);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedDate);
        });
        modelBuilder.Entity<TypeOfUseGroupEntity>(entity =>
        {
            entity.ToTable("TypeOfUseGroupMaster", "PTIS");
            entity.HasKey(x => x.TypeOfUseGroupID);
            entity.Property(x => x.GroupName).IsRequired().HasMaxLength(50);
            entity.Property(x => x.GroupNameEnglish).HasMaxLength(50);
            entity.Property(x => x.GroupIcon).HasMaxLength(20);
            entity.Property(x => x.CreatedBy);
            entity.Property(x => x.CreatedDate);
            entity.Property(x => x.UpdatedBy);
            entity.Property(x => x.UpdatedDate);
            entity.Property(x => x.IsActive);
        });

        modelBuilder.Entity<ZoneEntity>(entity =>
        {
            entity.ToTable("ZoneMaster", "PTIS");
            entity.HasKey(x => x.ZoneNo);
            entity.Property(x => x.ZoneNo);
            entity.Property(x => x.Description);
            entity.Property(x => x.DescriptionEnglish);
            entity.Property(x => x.SequenceNo);
            entity.Property(x => x.IsActive);
        });
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                  entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientType).HasMaxLength(50);
            entity.Property(e => e.DeviceInfo).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.TokenHash);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // LoginAttempt configuration
        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.ToTable("LoginAttempts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.AuthProvider).HasMaxLength(50);
            entity.Property(e => e.ClientType).HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany(u => u.LoginAttempts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => e.IpAddress);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Organization configuration (minimal entity)
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsSetupComplete).IsRequired();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // OrganizationSetting configuration (key-value store)
        modelBuilder.Entity<OrganizationSetting>(entity =>
        {
            entity.ToTable("OrganizationSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AuthProvider configuration
        modelBuilder.Entity<AuthProvider>(entity =>
        {
            entity.ToTable("AuthProviders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConfigJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.ProviderType);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // FeatureFlag configuration
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.ModuleName).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // MultilingualDetail configuration
        modelBuilder.Entity<MultilingualDetailsEntity>(entity =>
        {
            entity.ToTable("MultilingualDetails", "PTIS");
            entity.HasIndex(x => new { x.Resource, x.Key, x.Culture })
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
            entity.HasKey(e => e.ID);
            entity.Property(e => e.ConstructionId).HasMaxLength(7);
            entity.Property(e => e.MinYear);
            entity.Property(e => e.MaxYear);
            entity.Property(e => e.Rate).HasColumnType("money");
            entity.Property(e => e.Year);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
        // TaxZone configuration
        modelBuilder.Entity<TaxZoneEntity>(entity =>
        {
            entity.ToTable("TaxZoneMaster", "PTIS");
            entity.HasKey(e => e.TaxZoneNo);
            entity.Property(e => e.TaxZoneType);
            entity.Property(e => e.Remark);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
        // UserRoleMaster configuration
        modelBuilder.Entity<UserRoleMasterEntity>(entity =>
        {
            entity.ToTable("UserRoleMaster", "Core");
            entity.HasKey(e => e.UserRoleId);
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
            entity.Property(e => e.Status).HasMaxLength(50);
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
        modelBuilder.Entity<YearMasterEntity>(entity =>
        {
            entity.ToTable("YearMaster", "Core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.YearCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
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
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.OfficeIncharge);
            entity.Property(e => e.Designation);
            entity.Property(e => e.EstablishedDate).HasColumnType("datetime");
            entity.Property(e => e.Status);
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
            entity.HasKey(x => x.RateSectionNo);
            entity.Property(x => x.RateSectionNo);
            entity.Property(x => x.Description);
            entity.Property(x => x.DescriptionEnglish);
           
        });
     modelBuilder.Entity<RateSectionDetailsEntity>(entity =>
        {
            entity.ToTable("RateSectionDetails", "PTIS");
            entity.HasKey(x => x.RateSectionDetailsID);
            entity.Property(x => x.RateSectionNo);
            entity.Property(x => x.WardNo);
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
            entity.HasKey(e => e.ScreenMasterId);
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
            entity.HasKey(e => e.ScreenGroupId);
        });
        modelBuilder.Entity<DepartmentMasterEntity>(entity =>
        {
            entity.ToTable("DepartmentMaster", "Core");
            entity.HasKey(e => e.DepartmentMasterId);
        }); 
         modelBuilder.Entity<ModuleMasterEntity>(entity =>
        {
            entity.ToTable("ModuleMaster", "Core");
              entity.HasKey(e => e.ModuleMasterId);
            entity.Property(e => e.DepartmentMasterId)
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
                .HasForeignKey(e => e.DepartmentMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DepartmentLicenceDetailsEntity>(entity =>
        {
            entity.ToTable("DepartmentLicenceDetails", "Core");
            entity.HasKey(e => e.LicenceDetailsId);
            entity.Property(e => e.LicenceDuration).HasMaxLength(50);
            // Configure relationship with DepartmentMaster
            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
		  modelBuilder.Entity<ModuleMasterEntity>(entity =>
        {
            entity.ToTable("ModuleMaster", "Core");
            entity.HasKey(e => e.ModuleMasterId);
            entity.Property(e => e.DepartmentMasterId)
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
                .HasForeignKey(e => e.DepartmentMasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DepartmentMasterEntity>(entity =>
        {
            entity.ToTable("DepartmentMaster", "Core");
            entity.HasKey(e => e.DepartmentMasterId);
            entity.Property(e => e.DepartmentCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DepartmentNameLocal).HasMaxLength(200);
            entity.Property(e => e.DepartmentIcon).HasMaxLength(100);
            entity.Property(e => e.DepartmentDescription).HasMaxLength(500);
            // Indexes
            entity.HasIndex(e => e.DepartmentMasterId);
            entity.HasIndex(e => e.IsActive);
        });

        // DesignationMasterEntity configuration
        modelBuilder.Entity<DesignationMasterEntity>(entity =>
        {
            entity.ToTable("DesignationMaster", "Core");
            entity.HasKey(e => e.DesignationMasterId);
            entity.Property(e => e.DesignationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DesignationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DesignationLocal).HasMaxLength(200);
            entity.Property(e => e.DesignationDescription).HasMaxLength(500);
            // Indexes
            entity.HasIndex(e => e.DesignationMasterId);
            entity.HasIndex(e => e.DesignationCode).IsUnique();
        });
        
        // ActiveTaxes configuration
        modelBuilder.Entity<ActiveTaxesEntity>(entity =>
        {
            entity.ToTable("ActiveTaxesMaster", "PTIS");
            entity.HasKey(x => x.TaxNameID);
            entity.Property(x => x.TaxNameID);
            entity.Property(x => x.TaxName);
            entity.Property(x => x.TaxNameAlias);
            entity.Property(x => x.TaxNameOrder);
            entity.Property(x => x.ActiveTaxHeadsOnly);
            entity.Property(x => x.DisplayOrder);
        });
         
    }
}
