namespace ValutaCore.Tests.Api;

public class ApiServiceRegistrationTests
{
    [Fact]
    public void AddApiServices_ShouldRegisterCoreSubsystems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());

        var jwtDict = new Dictionary<string, string>
        {
            ["JwtSettings:Secret"]        = "test-secret-key-with-minimum-length-for-security",
            ["JwtSettings:Issuer"]        = "test-issuer",
            ["JwtSettings:Audience"]      = "test-audience",
            ["JwtSettings:ExpiryMinutes"] = "60"
        };

        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(jwtDict)
            .Build();

        var initialCount = services.Count;

        // Act
        services.AddApiServices(cfg);

        // Assert – simple count increase
        Assert.True(services.Count > initialCount);

        // Controllers / MVC
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Controller") == true);

        // Versioning
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("ApiVersion") == true);

        // Swagger
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Swagger") == true);

        // Auth & JWT
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Authentication") == true &&
                                       d.ServiceType.FullName.Contains("Jwt"));

        // MemoryCache
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("MemoryCache") == true);

        // OpenTelemetry
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("OpenTelemetry") == true);
    }

    [Fact]
    public void AddApiServices_ShouldConfigureMvcAndJson()
    {
        var services = new ServiceCollection();
        services.AddApiServices(new ConfigurationBuilder().Build());

        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Mvc")  == true);
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Json") == true);
    }

    [Fact]
    public void AddApiServices_ShouldConfigureJwtAuthentication()
    {
        var services = new ServiceCollection();
        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:Secret"]        = "secret-12345678901234567890",
                ["JwtSettings:Issuer"]        = "issuer",
                ["JwtSettings:Audience"]      = "audience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            }!)
            .Build();

        services.AddApiServices(cfg);

        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Authentication") == true &&
                                       d.ServiceType.FullName.Contains("Jwt"));
    }

    [Fact]
    public void AddApiServices_ShouldRegisterSwaggerAndApiExplorer()
    {
        var services = new ServiceCollection();
        services.AddApiServices(new ConfigurationBuilder().Build());

        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("Swagger") == true);
        Assert.Contains(services, d => d.ServiceType.FullName?.Contains("ApiExplorer") == true);
    }

    [Fact]
    public void UseApiMiddleware_ShouldReturnSameBuilder()
    {
        // Using a real ApplicationBuilder avoids NullReference inside UseMiddleware
        var provider   = new ServiceCollection().BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(provider);

        var result = appBuilder.UseApiMiddleware();

        Assert.Same(appBuilder, result);
    }
}
