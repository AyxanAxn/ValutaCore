namespace ValutaCore.IntegrationTests;

public class ValutaCoreApiIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly FrankfurterApiProvider _provider;

    public ValutaCoreApiIntegrationTests()
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
    public async Task RetrieveLatestRatesAsync_WithValidBaseCurrency_ReturnsRates()
    {
        var result = await _provider.RetrieveLatestRatesAsync("USD");

        Assert.NotNull(result);
        Assert.Equal("USD", result.BaseCurrency);
        Assert.NotEmpty(result.Rates);
    }

    [Fact]
    public async Task PerformConversionAsync_WithValidParameters_ReturnsConversion()
    {
        var result = await _provider.PerformConversionAsync(100m, "USD", "EUR");

        Assert.NotNull(result);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.BaseCurrency);
        Assert.True(result.Rates.ContainsKey("EUR"));
        Assert.True(result.Rates["EUR"] > 0);
    }

    [Fact]
    public async Task RetrieveHistoricalRatesAsync_WithValidDateRange_ReturnsHistoricalData()
    {
        var startDate = DateTime.Today.AddDays(-5);
        var endDate   = DateTime.Today;

        var result = await _provider.RetrieveHistoricalRatesAsync("USD", startDate, endDate);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var businessDays = CountBusinessDays(startDate, endDate);
        Assert.True(result.Count <= businessDays);

        foreach (var kvp in result)
        {
            Assert.InRange(kvp.Key, startDate, endDate);
            Assert.NotEmpty(kvp.Value);
        }
    }

    [Fact]
    public async Task RetrieveLatestRatesAsync_WithInvalidBaseCurrency_ThrowsHttpRequestException()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _provider.RetrieveLatestRatesAsync("INVALID"));
    }

    private static int CountBusinessDays(DateTime start, DateTime end)
    {
        int days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        }
        return days;
    }
}
