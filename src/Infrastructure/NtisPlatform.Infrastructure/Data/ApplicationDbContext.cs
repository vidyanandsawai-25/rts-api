using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Infrastructure.Data;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SampleEntity> SampleEntities { get; set; } = null!;
    public DbSet<PTISConstructionTypeMasterEntity> PTISConstructionTypeMasterEntities { get; set; } = null!;
    public DbSet<PTISFloorMasterEntity> PTISFloorMasterEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- SampleEntity ----------------
        modelBuilder.Entity<SampleEntity>(entity =>
        {
            entity.ToTable("SampleEntities");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Description)
                  .HasMaxLength(1000);

            entity.Property(e => e.CreatedBy)
                  .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                  .HasMaxLength(100);

        });

        // ---------------- PTISMasterEntity ----------------
        modelBuilder.Entity<PTISConstructionTypeMasterEntity>(entity =>
        {
            entity.ToTable("ConstructionTypeMaster", "PTIS");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ConstructionId)
                  .HasMaxLength(200);

            entity.Property(e => e.Description)
                  .HasMaxLength(1000);

    
            entity.Property(e => e.CreatedBy)
                  .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                  .HasMaxLength(100);

            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedDate);

        });

        modelBuilder.Entity<PTISFloorMasterEntity>(entity =>
        {
            entity.ToTable("FloorMaster", "PTIS");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FloorID).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

        });
    }

    

}
