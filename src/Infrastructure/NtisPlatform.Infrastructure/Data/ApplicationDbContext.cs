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
    public DbSet<TypeOfUseGroupEntity> TypeOfUseGroup { get; set; } = null!;
    public DbSet<DepreciationMasterEntity> DepreciationMaster { get; set; } = null!;
    public DbSet<YearMasterEntity> YearMasterEntity { get; set; } = null!;
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

        modelBuilder.Entity<SubFloorEntity>(entity =>
        {
            entity.ToTable("SubFloorMaster", "PTIS");
            entity.HasKey(e => e.SubFloorId);
            entity.Property(e => e.SubFloorDescription);
            entity.Property(e => e.SubFloorDescriptionEnglish);
            entity.Property(e => e.SubFloorPercentage);

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
           entity.HasKey(e => e.ID);
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

        // YearMaster configuration
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
    }
}
