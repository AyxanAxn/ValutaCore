using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using ValutaCore.Infrastructure;
using ValutaCore.Api.Middleware;
using ValutaCore.Core;
using Serilog.Events;
using ValutaCore.Api;
using Serilog;
using ValutaCore.Application;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)           // keep the noise filter …
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information) // …but allow the port banner
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("global-logs/valuta-core-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();                 // keep default providers so hosting log appears

builder.Services
    .AddCoreServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration)
    // MediatR 11.x style: pass the assembly directly
    .AddMediatR(typeof(AssemblyMarker).Assembly)
    .AddValidatorsFromAssemblyContaining<AssemblyMarker>();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Log.Information("Now listening on: {urls}", string.Join(", ", app.Urls));
});

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