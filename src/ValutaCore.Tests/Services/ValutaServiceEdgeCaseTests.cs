namespace ValutaCore.Tests.Services;

public class ValutaServiceEdgeCaseTests
{
    private readonly Mock<IExchangeProvider>          _exchangeSourceMock;
    private readonly Mock<IMapper>                    _mapperMock;
    private readonly IMemoryCache                     _cache;
    private readonly ValutaService                    _sut;

    public ValutaServiceEdgeCaseTests()
    {
        var providerFactoryMock = new Mock<IExchangeProviderFactory>();
        _exchangeSourceMock     = new Mock<IExchangeProvider>();
        var loggerMock          = new Mock<ILogger<ValutaService>>();
        _mapperMock             = new Mock<IMapper>();
        _cache                  = new MemoryCache(new MemoryCacheOptions());

        providerFactoryMock
            .Setup(f => f.GetProvider(It.IsAny<string>()))
            .Returns(_exchangeSourceMock.Object);

        // ← add this:
        var restricted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _sut = new ValutaService(
            providerFactoryMock.Object,
            _cache,
            loggerMock.Object,
            _mapperMock.Object,
            restricted    // ← supply the new parameter here
        );
    }


    // ----------- GetLatestRatesAsync -----------------------------------------

    [Fact]
    public async Task GetLatestRates_NullBaseCurrency_ThrowsArgumentException() =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetLatestRatesAsync(null!));

    [Fact]
    public async Task GetLatestRates_EmptyBaseCurrency_ThrowsArgumentException() =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetLatestRatesAsync(string.Empty));

    [Fact]
    public async Task GetLatestRates_ProviderThrows_PropagatesException()
    {
        const string usd = "USD";
        _exchangeSourceMock
            .Setup(p => p.RetrieveLatestRatesAsync(usd))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.GetLatestRatesAsync(usd));

        Assert.Equal("API unavailable", ex.Message);
    }

    [Fact]
    public async Task GetLatestRates_CachedValue_IsReturnedWithoutProviderCall()
    {
        const string usd = "USD";
        var cached = new ExchangeRateResponse
        {
            Amount       = 1,
            BaseCurrency = usd,
            Date         = DateTime.Today,
            Rates        = new() { ["EUR"] = 0.85m }
        };

        _cache.Set(CacheKeys.LatestRates(usd), cached, TimeSpan.FromHours(1));

        var result = await _sut.GetLatestRatesAsync(usd);

        Assert.Equal(cached, result);
        _exchangeSourceMock.Verify(
            p => p.RetrieveLatestRatesAsync(It.IsAny<string>()),
            Times.Never);
    }

    // ----------- ConvertCurrencyAsync ----------------------------------------

    [Fact]
    public async Task ConvertCurrency_NullRequest_ThrowsArgumentNullException() =>
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ConvertCurrencyAsync(null!));

    [Fact]
    public async Task ConvertCurrency_NullSourceCurrency_ThrowsArgumentException()
    {
        var req = new ExchangeRequest { Amount = 100, SourceCurrency = null!, TargetCurrency = "EUR" };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ConvertCurrencyAsync(req));
    }

    [Fact]
    public async Task ConvertCurrency_NullTargetCurrency_ThrowsArgumentException()
    {
        var req = new ExchangeRequest { Amount = 100, SourceCurrency = "USD", TargetCurrency = null! };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ConvertCurrencyAsync(req));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ConvertCurrency_NonPositiveAmount_ThrowsArgumentException(decimal amount)
    {
        var req = new ExchangeRequest { Amount = amount, SourceCurrency = "USD", TargetCurrency = "EUR" };
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ConvertCurrencyAsync(req));
    }

    [Fact]
    public async Task ConvertCurrency_CachedValue_IsReturnedWithoutProviderCall()
    {
        var req = new ExchangeRequest { Amount = 100, SourceCurrency = "USD", TargetCurrency = "EUR" };

        var cached = new ExchangeRateResponse
        {
            Amount       = 1,
            BaseCurrency = "USD",
            Date         = DateTime.Today,
            Rates        = new() { ["EUR"] = 0.85m }
        };

        var expected = new ValutaCoreResponse
        {
            Amount          = 100,
            FromCurrency    = "USD",
            ToCurrency      = "EUR",
            ConvertedAmount = 85m,
            Rate            = 0.85m,
            Date            = DateTime.Today
        };

        _cache.Set(CacheKeys.ConversionRate("USD", "EUR"), cached, TimeSpan.FromHours(1));

        _mapperMock
            .Setup(m => m.Map<ValutaCoreResponse>(It.IsAny<object>()))
            .Returns(expected);

        var result = await _sut.ConvertCurrencyAsync(req);

        Assert.Equal(expected, result);
        _exchangeSourceMock.Verify(
            p => p.PerformConversionAsync(
                   It.IsAny<decimal>(),
                   It.IsAny<string>(),
                   It.IsAny<string>()),   // ← now includes a token
            Times.Never);
    }

    // ----------- GetHistoricalRatesAsync -------------------------------------

    [Fact]
    public async Task GetHistoricalRates_NullRequest_ThrowsArgumentNullException() =>
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.GetHistoricalRatesAsync(null!));

    [Fact]
    public async Task GetHistoricalRates_NullBaseCurrency_ThrowsArgumentException()
    {
        var req = new HistoricalRatesRequest
        {
            BaseCurrency = null!,
            StartDate    = new DateTime(2020, 1, 1),
            EndDate      = new DateTime(2020, 1, 5),
            Page         = 1,
            PageSize     = 10
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetHistoricalRatesAsync(req));
    }

    [Fact]
    public async Task GetHistoricalRates_EndDateBeforeStart_ThrowsArgumentException()
    {
        var req = new HistoricalRatesRequest
        {
            BaseCurrency = "USD",
            StartDate    = new DateTime(2020, 1, 5),
            EndDate      = new DateTime(2020, 1, 1),
            Page         = 1,
            PageSize     = 10
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetHistoricalRatesAsync(req));
    }

    [Fact]
    public async Task GetHistoricalRates_NegativePage_SetsPageToOne()
    {
        var req = new HistoricalRatesRequest
        {
            BaseCurrency = "USD",
            StartDate    = new DateTime(2020, 1, 1),
            EndDate      = new DateTime(2020, 1, 5),
            Page         = -5,
            PageSize     = 10
        };

        var rawData = new Dictionary<DateTime, Dictionary<string, decimal>>
        {
            [new DateTime(2020, 1, 1)] = new() { ["EUR"] = 0.85m }
        };

        // ← include the CancellationToken in the setup
        _exchangeSourceMock
            .Setup(p => p.RetrieveHistoricalRatesAsync(
                "USD",
                req.StartDate,
                req.EndDate))
            .ReturnsAsync(rawData);

        _mapperMock
            .Setup(m => m.Map<PaginatedResponse<RateHistoryEntry>>(It.IsAny<object>()))
            .Returns(new PaginatedResponse<RateHistoryEntry> { Page = 1 });

        var result = await _sut.GetHistoricalRatesAsync(req);

        Assert.Equal(1, result.Page); // page corrected to 1
    }
}
