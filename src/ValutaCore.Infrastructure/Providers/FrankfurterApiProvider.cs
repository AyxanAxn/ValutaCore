namespace ValutaCore.Infrastructure.Providers;

public class FrankfurterApiProvider : IExchangeProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterApiProvider> _logger;
    private const string BaseUrl = "https://api.frankfurter.app";

    public string ProviderIdentifier => "Frankfurter";

    public FrankfurterApiProvider(HttpClient httpClient, ILogger<FrankfurterApiProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<ExchangeRateResponse> RetrieveLatestRatesAsync(string baseCurrency)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>($"/latest?from={baseCurrency}");
            return response!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest rates for base currency: {BaseCurrencyCode}", baseCurrency);
            throw;
        }
    }

    public async Task<ExchangeRateResponse> PerformConversionAsync(decimal amount,
        string fromCurrency,
        string toCurrency)
    {
        try
        {
            var response =
                await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(
                    $"/latest?amount={amount}&from={fromCurrency}&to={toCurrency}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting {Amount} from {SourceCurrency} to {TargetCurrency}", amount, fromCurrency,
                toCurrency);
            throw;
        }
    }

    public async Task<Dictionary<DateTime, Dictionary<string, decimal>>>
        RetrieveHistoricalRatesAsync(string baseCurrency,
            DateTime startDate,
            DateTime endDate)
    {
        try
        {
            var url = $"/{startDate.ToString("yyyy-MM-dd")}..{endDate.ToString("yyyy-MM-dd")}?from={baseCurrency}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var historicalData = JsonSerializer.Deserialize<HistoricalRatesResponse>(content, options);
            return historicalData.Rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching historical rates for base currency: {BaseCurrencyCode} from {StartDate} to {EndDate}",
                baseCurrency, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private class HistoricalRatesResponse
    {
        public decimal Amount { get; init; }
        public string Base { get; init; }
        public DateTime Start_Date { get; init; }
        public DateTime End_Date { get; init; }
        public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }
    }
}