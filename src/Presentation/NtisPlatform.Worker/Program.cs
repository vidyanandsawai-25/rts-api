using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Worker;
using NtisPlatform.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "NTIS Platform Worker Service";
});

// Infrastructure Layer - Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// Infrastructure Layer - Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Layer - Services
builder.Services.AddScoped<IHardDeleteCleanupService, HardDeleteCleanupService>();

// Add background workers
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<HardDeleteCleanupWorker>();

var host = builder.Build();
host.Run();
