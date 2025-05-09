namespace ValutaCore.Tests;

public class ServiceRegistrationTests
{
    // ─────────────────────────── API  ────────────────────────────

    [Fact]
    public void AddApiServices_RegistersCoreComponents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:Secret"]        = "test-secret-key-with-minimum-length-for-security",
                ["JwtSettings:Issuer"]        = "test-issuer",
                ["JwtSettings:Audience"]      = "test-audience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            }!)
            .Build();

        var beforeCount = services.Count;

        // Act
        services.AddApiServices(cfg);

        // Assert
        Assert.True(services.Count > beforeCount, "Expected new registrations");

        Assert.Contains(services, s => s.ServiceType.Name.Contains("Controller") ||
                                       s.ServiceType.FullName!.Contains("Swagger") ||
                                       s.ServiceType.FullName!.Contains("Mvc"));
    }

    [Fact]
    public void UseApiMiddleware_ReturnsSameBuilder()
    {
        // Arrange – real builder to avoid NREs inside UseMiddleware
        var provider   = new ServiceCollection().BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(provider);

        // Act / Assert
        Assert.Same(appBuilder, appBuilder.UseApiMiddleware());
    }

    [Fact]
    public void AddApiServices_RegistersJwtAuthentication()
    {
        var services = new ServiceCollection();
        services.AddApiServices(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:Secret"]        = "test-secret-key-with-minimum-length-for-security",
                ["JwtSettings:Issuer"]        = "issuer",
                ["JwtSettings:Audience"]      = "audience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            }!)
            .Build());

        Assert.Contains(services, s => s.ServiceType.FullName!.Contains("Authentication") &&
                                       s.ServiceType.FullName.Contains("Jwt"));
    }

    [Fact]
    public void AddApiServices_RegistersSwaggerAndApiExplorer()
    {
        var services = new ServiceCollection();
        services.AddApiServices(new ConfigurationBuilder().Build());

        Assert.Contains(services, s => s.ServiceType.FullName!.Contains("Swagger"));
        Assert.Contains(services, s => s.ServiceType.Name == "IApiVersionDescriptionProvider");
    }

    // ─────────────────────────── CORE  ────────────────────────────

    [Fact]
    public void AddCoreServices_RegistersMappingEtc()
    {
        var services     = new ServiceCollection();
        var beforeCount  = services.Count;

        services.AddCoreServices();

        Assert.True(services.Count > beforeCount);
        Assert.Contains(services, s => s.ServiceType.FullName!.Contains("Mapper") ||
                                       s.ServiceType.FullName.Contains("Mapster"));
    }

    // ────────────────────── Infrastructure  ───────────────────────

    [Fact]
    public void AddInfrastructureServices_RegistersInfrastructureComponents()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:Secret"]        = "test-secret-key-with-minimum-length-for-security",
                ["JwtSettings:Issuer"]        = "issuer",
                ["JwtSettings:Audience"]      = "audience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            }!)
            .Build();

        var before = services.Count;

        services.AddInfrastructureServices(cfg);

        Assert.True(services.Count > before);
        Assert.Contains(services, s => s.ServiceType == typeof(IJwtTokenService));
        Assert.Contains(services, s => s.ServiceType == typeof(IExchangeProviderFactory));
    }

    [Fact]
    public void CreateResiliencePolicy_ReturnsPolicyInstance()
    {
        var loggerMock = new Mock<ILogger<FrankfurterApiProvider>>();

        var factoryMethod = typeof(RegisterInfrastructure).GetMethod(
            "CreateResiliencePolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var policyObj = factoryMethod.Invoke(null, new object[] { loggerMock.Object });

        Assert.NotNull(policyObj);
    }

    [Fact]
    public void AddInfrastructureServices_RegistersHttpClientAndProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JwtSettings:Secret"]        = "test-secret-key-with-minimum-length-for-security",
                ["JwtSettings:Issuer"]        = "issuer",
                ["JwtSettings:Audience"]      = "audience",
                ["JwtSettings:ExpiryMinutes"] = "60"
            }!)
            .Build();

        services.AddInfrastructureServices(cfg);

        Assert.Contains(services, s => s.ServiceType.Name.Contains("HttpClient"));
        Assert.Contains(services, s => s.ServiceType == typeof(FrankfurterApiProvider));
        Assert.Contains(services, s => s.ServiceType == typeof(IExchangeProvider));
    }
}
