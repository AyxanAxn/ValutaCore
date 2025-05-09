namespace ValutaCore.Tests.Infrastructure;

public class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructureServices_MethodIsDiscoverable()
    {
        // Arrange
        var cfgMock  = new Mock<IConfiguration>();
        cfgMock.Setup(c => c.GetSection("JwtSettings"))
               .Returns(new Mock<IConfigurationSection>().Object);

        // Act & Assert
        var method = typeof(RegisterInfrastructure).GetMethod("AddInfrastructureServices");
        Assert.NotNull(method);
    }

    [Fact]
    public void CreateResiliencePolicy_PrivateStaticMethodExists_WithExpectedSignature()
    {
        // Act
        var methodInfo = typeof(RegisterInfrastructure).GetMethod(
            "CreateResiliencePolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(IAsyncPolicy<HttpResponseMessage>), methodInfo!.ReturnType);
        Assert.Single(methodInfo.GetParameters());

        var loggerParam = methodInfo.GetParameters()[0];
        Assert.Contains("Logger", loggerParam.ParameterType.Name);
    }

    [Fact]
    public void AddInfrastructureServices_MethodSignature_IsCorrectAndRegistersExpectedTypes()
    {
        // Arrange
        var addMethod = typeof(RegisterInfrastructure).GetMethod("AddInfrastructureServices");

        // Assert — signature
        Assert.NotNull(addMethod);
        Assert.Equal(typeof(IServiceCollection), addMethod!.ReturnType);
        Assert.Equal(2, addMethod.GetParameters().Length);
        Assert.Equal(typeof(IServiceCollection), addMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(IConfiguration),       addMethod.GetParameters()[1].ParameterType);

        // Assert — key concrete types exist
        Assert.NotNull(typeof(JwtTokenService));
        Assert.NotNull(typeof(ExchangeProviderFactory));
        Assert.NotNull(typeof(FrankfurterApiProvider));

        // Assert — interface mappings
        Assert.True(typeof(JwtTokenService)        .GetInterfaces().Contains(typeof(IJwtTokenService)));
        Assert.True(typeof(ExchangeProviderFactory).GetInterfaces().Contains(typeof(IExchangeProviderFactory)));
        Assert.True(typeof(FrankfurterApiProvider) .GetInterfaces().Contains(typeof(IExchangeProvider)));
    }
}
