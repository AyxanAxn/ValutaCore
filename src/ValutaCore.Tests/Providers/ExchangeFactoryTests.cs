namespace ValutaCore.Tests.Providers;

public class ExchangeFactoryTests
{
    private readonly Mock<IServiceProvider>  _serviceProviderMock;
    private readonly Mock<IExchangeProvider> _frankfurterMock;
    private readonly ExchangeProviderFactory _factory;

    public ExchangeFactoryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _frankfurterMock     = new Mock<IExchangeProvider>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(FrankfurterApiProvider)))
            .Returns(_frankfurterMock.Object);

        _factory = new ExchangeProviderFactory(_serviceProviderMock.Object);
    }

    [Fact]
    public void GetExchange_DefaultName_ReturnsFrankfurter()
    {
        // Act
        var exchange = _factory.GetProvider();

        // Assert
        Assert.Same(_frankfurterMock.Object, exchange);
        _serviceProviderMock.Verify(sp => sp.GetService(typeof(FrankfurterApiProvider)), Times.Once);
    }

    [Fact]
    public void GetExchange_WithExplicitFrankfurterName_ReturnsFrankfurter()
    {
        var exchange = _factory.GetProvider("Frankfurter");

        Assert.Same(_frankfurterMock.Object, exchange);
        _serviceProviderMock.Verify(sp => sp.GetService(typeof(FrankfurterApiProvider)), Times.Once);
    }

    [Fact]
    public void GetExchange_IsCaseInsensitive_ReturnsFrankfurter()
    {
        var exchange = _factory.GetProvider("frankfurter");

        Assert.Same(_frankfurterMock.Object, exchange);
        _serviceProviderMock.Verify(sp => sp.GetService(typeof(FrankfurterApiProvider)), Times.Once);
    }

    [Fact]
    public void GetExchange_UnknownName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => _factory.GetProvider("UnknownSource"));
        Assert.Contains("is not supported", ex.Message);
    }

    [Fact]
    public void GetAvailableExchanges_ReturnsExpectedList()
    {
        var names = _factory.GetAvailableProviders().ToList();

        Assert.Single(names);
        Assert.Contains("Frankfurter", names);
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ExchangeProviderFactory(null!));
    }
}
