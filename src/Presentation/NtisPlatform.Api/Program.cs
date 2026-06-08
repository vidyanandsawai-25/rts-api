using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server options for file upload limits (from the strongly-typed FileStorage options)
var fileStorageOptions = builder.Configuration.GetSection(NtisPlatform.Application.Options.FileStorageOptions.Section)
    .Get<NtisPlatform.Application.Options.FileStorageOptions>() ?? new NtisPlatform.Application.Options.FileStorageOptions();
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = fileStorageOptions.MaxFileSizeBytes;
});

// Register all services in one place
builder.Services.AddAllServices(builder.Configuration);

// Localization from .resx
/*
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllers().AddDataAnnotationsLocalization();
*/

// Localization from Database + Cache 
builder.Services.AddMemoryCache();

var app = builder.Build();

// Global exception handling
app.UseMiddleware<NtisPlatform.Api.Middleware.GlobalExceptionHandlerMiddleware>();

// Language middleware - extracts language from Accept-Language header
app.UseMiddleware<NtisPlatform.Api.Middleware.LanguageMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must come before authentication
app.UseCors("AllowAll");

// Rate limiting - protects against brute force attacks (only if enabled in config)
var rateLimitingEnabled = builder.Configuration.GetValue<bool>("RateLimiting:Enabled", false);
if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
