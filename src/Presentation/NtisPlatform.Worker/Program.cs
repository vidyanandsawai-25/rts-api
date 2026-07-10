using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Worker;
using NtisPlatform.Worker.Services;
using NtisPlatform.Application.Options;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "NTIS Platform Worker Service";
});

// Infrastructure Layer - Database
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// Register scoped DbContext for DI (created from factory)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    return factory.CreateDbContext();
});

// Report queue DB (separate database) used by the retention sweep worker
builder.Services.AddPooledDbContextFactory<ReportingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReportingConnection")));
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>();
    return factory.CreateDbContext();
});

// Infrastructure Layer - Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Async reporting options (SLT/LLT lifetimes, retention sweep)
builder.Services.Configure<ReportingOptions>(builder.Configuration.GetSection(ReportingOptions.Section));

// Infrastructure Layer - Localization
builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection("Localization"));
builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

// Application Layer - Services
builder.Services.Configure<NtisPlatform.Application.Options.FileStorageOptions>(
    builder.Configuration.GetSection(NtisPlatform.Application.Options.FileStorageOptions.Section));
builder.Services.AddScoped<IHardDeleteCleanupService, HardDeleteCleanupService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Add background workers
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<HardDeleteCleanupWorker>();
// StaleReportRequestReclaimWorker retired Hangfire's invisibility timeout + server heartbeat now
// reclaim jobs whose worker died, so a separate lease-sweep is no longer needed.
builder.Services.AddHostedService<ReportRetentionWorker>();

var host = builder.Build();
host.Run();
