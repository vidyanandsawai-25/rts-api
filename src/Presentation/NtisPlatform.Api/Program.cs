using NtisPlatform.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register all services in one place
builder.Services.AddAllServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// TODO: Uncomment when authentication is configured
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();
