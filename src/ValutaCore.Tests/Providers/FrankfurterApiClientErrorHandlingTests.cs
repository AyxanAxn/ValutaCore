// FrankfurterApiProvider lives here

namespace ValutaCore.Tests.Providers;

public class FrankfurterApiClientErrorHandlingTests
{
    private readonly Mock<ILogger<FrankfurterApiProvider>> _loggerMock;
    private readonly Mock<HttpMessageHandler>              _handlerMock;
    private readonly HttpClient                            _httpClient;
    private readonly FrankfurterApiProvider                _client;

    public FrankfurterApiClientErrorHandlingTests()
    {
        _loggerMock  = new Mock<ILogger<FrankfurterApiProvider>>();
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app")
        };

        _client = new FrankfurterApiProvider(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task PerformConversionAsync_WhenServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.PerformConversionAsync(100m, "USD", "EUR"));
    }

    [Fact]
    public async Task RetrieveHistoricalRatesAsync_WhenServerError_ThrowsHttpRequestException()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.RetrieveHistoricalRatesAsync("USD", new DateTime(2020, 1, 1), new DateTime(2020, 1, 5)));
    }

    [Fact]
    public async Task RetrieveLatestRatesAsync_WithInvalidJson_ThrowsJsonException()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent("{ invalid json }")
            });

        await Assert.ThrowsAsync<JsonException>(() => _client.RetrieveLatestRatesAsync("USD"));
    }

    [Fact]
    public async Task PerformConversionAsync_WithInvalidJson_ThrowsJsonException()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent("{ invalid json }")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            _client.PerformConversionAsync(100m, "USD", "EUR"));
    }

    [Fact]
    public async Task RetrieveHistoricalRatesAsync_WithInvalidJson_ThrowsJsonException()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent("{ invalid json }")
            });

        await Assert.ThrowsAsync<JsonException>(() =>
            _client.RetrieveHistoricalRatesAsync("USD", new DateTime(2020, 1, 1), new DateTime(2020, 1, 5)));
    }
}
