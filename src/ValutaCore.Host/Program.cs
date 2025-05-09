using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using ValutaCore.Core.Configuration;
using ValutaCore.Infrastructure;
using ValutaCore.Api;
using ValutaCore.Api.Middleware;
using ValutaCore.Application;
using ValutaCore.Core;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("global-logs/valuta-core-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Serilog for host & app
builder.Host.UseSerilog();

// 2️⃣ (Optional) Bind the whole settings object if you plan to inject ValutaSettings elsewhere
builder.Services.Configure<ValutaSettings>(
    builder.Configuration.GetSection("ValutaSettings")
);




var restrictedSection = builder.Configuration.GetSection("ValutaSettings:RestrictedCurrencies");

var restrictedArray = (from child in restrictedSection.GetChildren() where !string.IsNullOrEmpty(child.Value) select child.Value).ToArray();

// Register it as a HashSet for all services
builder.Services.AddSingleton(new HashSet<string>(
    restrictedArray,
    StringComparer.OrdinalIgnoreCase
));

// 5️⃣ The rest of your registrations
builder.Services
    .AddCoreServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddMediatR(typeof(AssemblyMarker).Assembly)  
    .AddValidatorsFromAssemblyContaining<AssemblyMarker>();

var app = builder.Build();

// Log where we’re listening
app.Lifetime.ApplicationStarted.Register(() =>
    Log.Information("Now listening on: {urls}", string.Join(", ", app.Urls))
);

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Converter API V1");
    o.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseApiMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();