namespace ValutaCore.Tests.Services
{
    public class ValutaServiceTests
    {
        private readonly Mock<IExchangeProvider>        _mockProvider;
        private readonly Mock<IMapper>                 _mockMapper;
        private readonly ValutaService                 _service;

        public ValutaServiceTests()
        {
            var mockProviderFactory = new Mock<IExchangeProviderFactory>();
            _mockProvider           = new Mock<IExchangeProvider>();
            var mockLogger          = new Mock<ILogger<ValutaService>>();
            _mockMapper             = new Mock<IMapper>();
            IMemoryCache cache      = new MemoryCache(new MemoryCacheOptions());

            mockProviderFactory
                .Setup(f => f.GetProvider(It.IsAny<string>()))
                .Returns(_mockProvider.Object);

            var restricted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRY", "PLN", "THB", "MXN"
            };

            _service = new ValutaService(
                mockProviderFactory.Object,
                cache,
                mockLogger.Object,
                _mockMapper.Object,
                restricted
            );
        }


        [Fact]
        public async Task GetLatestRatesAsync_WithValidBaseCurrency_ReturnsFilteredRates()
        {
            const string baseCurrency = "USD";
            var expectedResponse = new ExchangeRateResponse
            {
                Amount       = 1,
                BaseCurrency = baseCurrency,
                Date         = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.75m },
                    { "JPY", 110.0m },
                    { "TRY", 8.5m }   // will be removed
                }
            };

            _mockProvider
                .Setup(p => p.RetrieveLatestRatesAsync(baseCurrency))
                .ReturnsAsync(expectedResponse);

            var result = await _service.GetLatestRatesAsync(baseCurrency);

            Assert.NotNull(result);
            Assert.Equal(baseCurrency, result.BaseCurrency);
            // The “TRY” entry should have been stripped
            Assert.Equal(3, result.Rates.Count);
            Assert.False(result.Rates.ContainsKey("TRY"));
            Assert.Equal(0.85m, result.Rates["EUR"]);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithRestrictedCurrency_ThrowsInvalidOperation()
        {
            // TRY is considered restricted in the default tests
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetLatestRatesAsync("TRY"));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithValidRequest_ReturnsMappedResponse()
        {
            var request = new ExchangeRequest
            {
                Amount         = 100,
                SourceCurrency = "USD",
                TargetCurrency = "EUR"
            };

            var conversionResponse = new ExchangeRateResponse
            {
                Amount       = 1,
                BaseCurrency = "USD",
                Date         = DateTime.Today,
                Rates        = new() { { "EUR", 0.85m } }
            };

            var expectedResult = new ValutaCoreResponse
            {
                Amount          = request.Amount,
                FromCurrency    = request.SourceCurrency,
                ToCurrency      = request.TargetCurrency,
                ConvertedAmount = 85.0m,
                Rate            = 0.85m,
                Date            = DateTime.Today
            };

            _mockProvider
                .Setup(p => p.PerformConversionAsync(1, request.SourceCurrency, request.TargetCurrency))
                .ReturnsAsync(conversionResponse);

            // The service passes a tuple (ExchangeRateResponse, decimal, string, string) to Map<>
            _mockMapper
                .Setup(m => m.Map<ValutaCoreResponse>(
                    It.Is<(ExchangeRateResponse, decimal, string, string)>(t =>
                        t.Item1 == conversionResponse &&
                        t.Item2 == request.Amount &&
                        t.Item3 == request.SourceCurrency &&
                        t.Item4 == request.TargetCurrency)))
                .Returns(expectedResult);

            var result = await _service.ConvertCurrencyAsync(request);

            Assert.Equal(expectedResult, result);

            _mockProvider.Verify(
                p => p.PerformConversionAsync(1, "USD", "EUR"),
                Times.Once);
            _mockMapper.Verify(
                m => m.Map<ValutaCoreResponse>(It.IsAny<(ExchangeRateResponse, decimal, string, string)>()),
                Times.Once);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithRestrictedTargetCurrency_ThrowsInvalidOperation()
        {
            var request = new ExchangeRequest
            {
                Amount         = 100,
                SourceCurrency = "USD",
                TargetCurrency = "TRY"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithValidRequest_ReturnsPaginatedResult()
        {
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate    = new DateTime(2020, 1, 1),
                EndDate      = new DateTime(2020, 1, 5),
                Page         = 1,
                PageSize     = 2
            };

            var rawData = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                [new DateTime(2020, 1, 1)] = new() { ["EUR"] = 0.85m, ["GBP"] = 0.75m, ["TRY"] = 8.5m },
                [new DateTime(2020, 1, 2)] = new() { ["EUR"] = 0.86m, ["GBP"] = 0.76m, ["TRY"] = 8.6m },
                [new DateTime(2020, 1, 3)] = new() { ["EUR"] = 0.87m, ["GBP"] = 0.77m, ["TRY"] = 8.7m }
            };

            _mockProvider
                .Setup(p => p.RetrieveHistoricalRatesAsync(
                    request.BaseCurrency, request.StartDate, request.EndDate))
                .ReturnsAsync(rawData);

            // The service eventually calls Map<PaginatedResponse<RateHistoryEntry>>
            var expectedPage = new PaginatedResponse<RateHistoryEntry>
            {
                Items      = new List<RateHistoryEntry>
                {
                    new RateHistoryEntry
                    {
                        Date = new DateTime(2020,1,1),
                        BaseCurrencyCode = "USD",
                        ExchangeRates = new Dictionary<string, decimal> { ["EUR"] = 0.85m, ["GBP"] = 0.75m }
                    },
                    new RateHistoryEntry
                    {
                        Date = new DateTime(2020,1,2),
                        BaseCurrencyCode = "USD",
                        ExchangeRates = new Dictionary<string, decimal> { ["EUR"] = 0.86m, ["GBP"] = 0.76m }
                    }
                },
                Page       = 1,
                PageSize   = 2,
                TotalCount = 3
            };

            _mockMapper
                .Setup(m => m.Map<PaginatedResponse<RateHistoryEntry>>(
                    It.Is<(HistoricalRatesRequest, List<RateHistoryEntry>)>(t => t.Item1 == request)))
                .Returns(expectedPage);

            var result = await _service.GetHistoricalRatesAsync(request);

            Assert.NotNull(result);
            Assert.Equal(expectedPage.Page,       result.Page);
            Assert.Equal(expectedPage.PageSize,   result.PageSize);
            Assert.Equal(expectedPage.TotalCount, result.TotalCount);
            Assert.Equal(2, result.Items.Count());

            foreach (var item in result.Items)
            {
                Assert.False(item.ExchangeRates.ContainsKey("TRY"));
                Assert.Equal(2, item.ExchangeRates.Count);
            }

            _mockMapper.Verify(
                m => m.Map<PaginatedResponse<RateHistoryEntry>>(It.IsAny<(HistoricalRatesRequest, List<RateHistoryEntry>)>()),
                Times.Once);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithRestrictedBaseCurrency_ThrowsInvalidOperation()
        {
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "PLN",
                StartDate    = new DateTime(2020, 1, 1),
                EndDate      = new DateTime(2020, 1, 5),
                Page         = 1,
                PageSize     = 10
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetHistoricalRatesAsync(request));
        }
    }
}
