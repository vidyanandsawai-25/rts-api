using NtisPlatform.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server options for file upload limits
var maxFileSizeBytes = builder.Configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSizeBytes;
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
