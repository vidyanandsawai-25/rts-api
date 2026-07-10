using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Enums;

namespace NtisPlatform.Infrastructure.Data;

/// <summary>
/// EF Core context for the separate report queue database (the ReportRequest queue).
///
/// IMPORTANT: this context only MAPS to tables that are created and owned by the separate
/// ntis database project. It must never create or migrate schema — there are no migrations and
/// no EnsureCreated. The Fluent configuration here must match the deployed table/column
/// definitions exactly.
/// </summary>
public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
    }

    public DbSet<ReportRequestEntity> ReportRequests { get; set; } = null!;
    public DbSet<ReportRequestLogEntity> ReportRequestLogs { get; set; } = null!;

    // Report catalogue (definitions + their parameters) — used by the UI to list reports and
    // render parameter forms. Moved here from the main PTIS database so the whole report surface
    // (catalogue + queue) lives in one database.
    public DbSet<ReportDefinitionEntity> ReportDefinitions { get; set; } = null!;
    public DbSet<ReportParameterDefinitionEntity> ReportParameterDefinitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReportRequestEntity>(entity =>
        {
            entity.ToTable("ReportRequest");
            entity.HasKey(e => e.ReportRequestId);
            entity.Property(e => e.ReportRequestId).ValueGeneratedNever();

            entity.Property(e => e.ReportCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParametersJson);
            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion(
                      v => v.ToString(),
                      v => (ReportRequestStatus)Enum.Parse(typeof(ReportRequestStatus), v));
            entity.Property(e => e.RequestedByUserId).IsRequired();
            entity.Property(e => e.OrganizationId);
            entity.Property(e => e.PlatformBaseUrl).IsRequired().HasMaxLength(500);

            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.StartedDate).HasColumnType("datetime2");
            entity.Property(e => e.CompletedDate).HasColumnType("datetime2");
            entity.Property(e => e.ErrorMessage);
            entity.Property(e => e.OutputDocumentGuid);

            entity.Property(e => e.ShortLivedToken).HasMaxLength(1000); // JWT SLT (was 128 for base64)
            entity.Property(e => e.SltExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.SltConsumed).IsRequired();

            entity.Property(e => e.AttemptCount).IsRequired();

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.ShortLivedToken)
                  .HasDatabaseName("IX_ReportRequest_Slt");
        });

        modelBuilder.Entity<ReportRequestLogEntity>(entity =>
        {
            entity.ToTable("ReportRequestLog");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ReportRequestId).IsRequired();
            entity.Property(e => e.FromStatus).HasMaxLength(20);
            entity.Property(e => e.ToStatus).HasMaxLength(20);
            entity.Property(e => e.Message);
            entity.Property(e => e.WorkerId).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnType("datetime2");

            entity.HasIndex(e => e.ReportRequestId)
                  .HasDatabaseName("IX_ReportRequestLog_ReportRequestId");
        });

        // Report catalogue — schema dbo (the report DB has no PTIS schema). Mirrors the table
        // definitions in db/NtisReportDb.sql; keep both in sync.
        modelBuilder.Entity<ReportDefinitionEntity>(entity =>
        {
            entity.ToTable("ReportDefinitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ReportCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReportName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TemplateFile).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataProviderCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.HasIndex(e => e.ReportCode).IsUnique().HasDatabaseName("UQ_ReportDefinitions_ReportCode");
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
        });

        modelBuilder.Entity<ReportParameterDefinitionEntity>(entity =>
        {
            entity.ToTable("ReportParameterDefinitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ReportDefinitionId).IsRequired();
            entity.Property(e => e.ParameterKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParameterType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CascadeFromKey).HasMaxLength(100);
            entity.Property(e => e.IsRequired).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.ReportDefinitionId, e.ParameterKey })
                  .IsUnique()
                  .HasDatabaseName("UQ_ReportParameterDefinitions_ReportId_Key");
            entity.HasIndex(e => e.ReportDefinitionId)
                  .HasDatabaseName("IX_ReportParameterDefinitions_ReportId");
            entity.HasIndex(e => new { e.ReportDefinitionId, e.IsActive, e.SortOrder })
                  .HasDatabaseName("IX_ReportParameterDefinitions_ReportId_Active_Sort");
        });
    }
}
