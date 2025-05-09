namespace ValutaCore.IntegrationTests;

public class ValutaCoreApiProviderIntegrationTests : IDisposable
{
    private readonly FrankfurterApiProvider _provider;
    private readonly HttpClient _httpClient;

    public ValutaCoreApiProviderIntegrationTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.frankfurter.app")
        };
        _provider = new FrankfurterApiProvider(
            _httpClient,
            NullLogger<FrankfurterApiProvider>.Instance);
    }

    public void Dispose() => _httpClient.Dispose();

    [Fact]
    public async Task RetrieveLatestRatesAsync_ShouldReturnRates_WhenBaseCurrencyIsValid()
    {
        // Arrange
        const string baseCurrency = "USD";

        // Act
        var result = await _provider.RetrieveLatestRatesAsync(baseCurrency);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(baseCurrency, result.BaseCurrency);
        Assert.NotEmpty(result.Rates);
    }

    [Fact]
    public async Task PerformConversionAsync_ShouldReturnConvertedAmount_WhenParametersAreValid()
    {
        // Arrange
        const decimal amount = 100m;
        const string fromCurrency = "USD";
        const string toCurrency   = "EUR";

        // Act
        var result = await _provider.PerformConversionAsync(amount, fromCurrency, toCurrency);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(fromCurrency, result.BaseCurrency);
        Assert.Contains(toCurrency, result.Rates.Keys);
        Assert.True(result.Rates[toCurrency] > 0);
    }
    
    [Fact]
    public async Task RetrieveLatestRatesAsync_ShouldThrowHttpRequestException_WhenBaseCurrencyIsInvalid()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _provider.RetrieveLatestRatesAsync("INVALID"));
    }

    private static int CountBusinessDays(DateTime start, DateTime end)
    {
        var days = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        return days;
    }
}
