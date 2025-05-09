namespace ValutaCore.Tests.Mapping
{
    public class CurrencyMappingProfileTests
    {
        private readonly TypeAdapterConfig _config;

        public CurrencyMappingProfileTests()
        {
            _config = new TypeAdapterConfig();
            new CurrencyMappingProfile().Register(_config);
        }

        [Fact]
        public void Map_ExchangeRateResponseTuple_ToCurrencyConversionResponse()
        {
            // Arrange
            var source = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = "USD",
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m }
                }
            };

            var tuple = (Source: source, Amount: 100m, FromCurrency: "USD", ToCurrency: "EUR");

            // Act
            var result = tuple.Adapt<ValutaCoreResponse>(_config);

            // Assert
            Assert.Equal(100m, result.Amount);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("EUR", result.ToCurrency);
            Assert.Equal(85m, result.ConvertedAmount);
            Assert.Equal(0.85m, result.Rate);
            Assert.Equal(DateTime.Today, result.Date);
        }

        [Fact]
        public void Map_ExchangeRateResponse_ToCurrencyConversionResponse()
        {
            // Arrange
            var source = new ExchangeRateResponse
            {
                Amount = 100m,
                BaseCurrency = "USD",
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m }
                }
            };

            // Act
            var result = source.Adapt<ValutaCoreResponse>(_config);

            // Assert
            Assert.Equal(100m, result.Amount);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("EUR", result.ToCurrency);
            Assert.Equal(85m, result.ConvertedAmount);
            Assert.Equal(0.85m, result.Rate);
            Assert.Equal(DateTime.Today, result.Date);
        }

        [Fact]
        public void Map_CurrencyConversionRequest_ToExchangeRateResponse()
        {
            // Arrange
            var source = new ExchangeRequest
            {
                Amount = 100m,
                SourceCurrency = "USD",
                TargetCurrency = "EUR"
            };

            // Act
            var result = source.Adapt<ExchangeRateResponse>(_config);

            // Assert
            Assert.Equal(100m, result.Amount);
            Assert.Equal("USD", result.BaseCurrency);
            Assert.Equal(DateTime.UtcNow.Date, result.Date.Date);
            Assert.Single(result.Rates);
            Assert.Contains("EUR", result.Rates.Keys);
            Assert.Equal(1.0m, result.Rates["EUR"]);
        }

        [Fact]
        public void Map_HistoricalDataTuple_ToHistoricalRatesList()
        {
            // Arrange
            var historicalData = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                {
                    new DateTime(2020, 1, 1),
                    new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m } }
                },
                {
                    new DateTime(2020, 1, 2),
                    new Dictionary<string, decimal> { { "EUR", 0.86m }, { "GBP", 0.76m } }
                }
            };

            var tuple = (Data: historicalData, BaseCurrency: "USD");

            // Act
            var result = tuple.Adapt<List<RateHistoryEntry>>(_config);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new DateTime(2020, 1, 1), result[0].Date);
            Assert.Equal("USD", result[0].BaseCurrencyCode);
            Assert.Equal(0.85m, result[0].ExchangeRates["EUR"]);
            Assert.Equal(0.75m, result[0].ExchangeRates["GBP"]);
            Assert.Equal(new DateTime(2020, 1, 2), result[1].Date);
        }

        [Fact]
        public void Map_HistoricalRatesRequestTuple_ToPaginatedResponse()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 5),
                Page = 1,
                PageSize = 2
            };

            var allRates = new List<RateHistoryEntry>
            {
                new RateHistoryEntry
                {
                    Date = new DateTime(2020, 1, 1),
                    BaseCurrencyCode = "USD",
                    ExchangeRates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
                },
                new RateHistoryEntry
                {
                    Date = new DateTime(2020, 1, 2),
                    BaseCurrencyCode = "USD",
                    ExchangeRates = new Dictionary<string, decimal> { { "EUR", 0.86m } }
                },
                new RateHistoryEntry
                {
                    Date = new DateTime(2020, 1, 3),
                    BaseCurrencyCode = "USD",
                    ExchangeRates = new Dictionary<string, decimal> { { "EUR", 0.87m } }
                }
            };

            var tuple = (Request: request, AllRates: allRates);

            // Act
            var result = tuple.Adapt<PaginatedResponse<RateHistoryEntry>>(_config);
            var itemsList = result.Items.ToList();

            // Assert
            Assert.Equal(1, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, itemsList.Count);
            Assert.Equal(new DateTime(2020, 1, 1), itemsList[0].Date);
            Assert.Equal(new DateTime(2020, 1, 2), itemsList[1].Date);
        }

        [Fact]
        public void Map_ConversionRequestTuple_ToCurrencyConversionResponse()
        {
            // Arrange
            var request = new ExchangeRequest
            {
                Amount = 100m,
                SourceCurrency = "USD",
                TargetCurrency = "EUR"
            };

            var tuple = (Request: request, Rate: 0.85m, ConvertedAmount: 85m, Date: DateTime.Today);

            // Act
            var result = tuple.Adapt<ValutaCoreResponse>(_config);

            // Assert
            Assert.Equal(100m, result.Amount);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("EUR", result.ToCurrency);
            Assert.Equal(85m, result.ConvertedAmount);
            Assert.Equal(0.85m, result.Rate);
            Assert.Equal(DateTime.Today, result.Date);
        }
    }
}
