using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ValutaCore.Tests;

/// <summary>
/// Light-weight “smoke” tests that ensure a WebApplication can be built and
/// basic service wiring completes without throwing.
/// </summary>
public class StartupSmokeTests
{
    [Fact]
    public void BuildWebApplication_WithDefaultPipeline_CompletesSuccessfully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Act
        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Assert – no exception means success
        Assert.NotNull(app);
    }

    [Fact]
    public void ConfigureServices_RegistersCoreFrameworkServices()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration cfg = new ConfigurationBuilder().Build();

        // Act – mimic Program.cs service wiring
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddAuthentication();
        services.AddAuthorization();

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}