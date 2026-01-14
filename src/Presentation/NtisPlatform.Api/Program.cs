using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Api.Middleware;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Resources;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Register all services in one place
builder.Services.AddAllServices(builder.Configuration);

// Localization from .resx
/*
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllers().AddDataAnnotationsLocalization();
*/

// Localization from Database + Cache 
builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddDataAnnotationsLocalization(options =>
    {
        // Always use DB resource: ValidationMessages
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create("ValidationMessages", location: null);
    });


// Supported cultures : These must match the "Culture" values stored in your (DB table/resx file)  - Example values: "en", "hi", "mr"
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("hi"),
    new CultureInfo("mr"),
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider(), // for Swagger/Postman
        new CookieRequestCultureProvider()                // for UI cookie selection
    };
});

var app = builder.Build();

// Global exception handling
app.UseMiddleware<NtisPlatform.Api.Middleware.GlobalExceptionHandlerMiddleware>();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must come before authentication
app.UseCors("AllowAll");

// Rate limiting - TEMPORARILY DISABLED FOR DEVELOPMENT
// app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
