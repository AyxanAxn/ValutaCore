namespace ValutaCore.Core.Services;

public class ValutaService(
    IExchangeProviderFactory providerFactory,
    IMemoryCache cache,
    ILogger<ValutaService> logger,
    IMapper mapper)
    : ICurrencyService
{
    private readonly IExchangeProviderFactory _providerFactory =
        providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));

    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<ValutaService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    private readonly HashSet<string> _restrictedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", "PLN", "THB", "MXN"
    };

    public bool IsRestrictedCurrency(string currency)
    {
        return _restrictedCurrencies.Contains(currency);
    }

    public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
    {
        if (string.IsNullOrEmpty(baseCurrency))
        {
            throw new ArgumentException("Base currency cannot be null or empty", nameof(baseCurrency));
        }

        if (_restrictedCurrencies.Contains(baseCurrency))
        {
            throw new InvalidOperationException($"Currency {baseCurrency} is restricted and cannot be used");
        }

        var cacheKey = CacheKeys.LatestRates(baseCurrency);
        if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse rates))
        {
            _logger.LogInformation("Cache miss for latest rates with base currency {BaseCurrencyCode}", baseCurrency);

            var provider = _providerFactory.GetProvider();
            rates = await provider.RetrieveLatestRatesAsync(baseCurrency);

            foreach (var restrictedCurrency in _restrictedCurrencies)
            {
                rates.Rates.Remove(restrictedCurrency);
            }

            _cache.Set(cacheKey, rates, TimeSpan.FromHours(1));
        }
        else
        {
            _logger.LogInformation("Cache hit for latest rates with base currency {BaseCurrencyCode}", baseCurrency);
        }

        return rates;
    }

    public async Task<ValutaCoreResponse> ConvertCurrencyAsync(ExchangeRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrEmpty(request.SourceCurrency) || string.IsNullOrEmpty(request.TargetCurrency))
        {
            throw new ArgumentException("Source and target currencies must be specified");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));
        }

        if (_restrictedCurrencies.Contains(request.SourceCurrency) || _restrictedCurrencies.Contains(request.TargetCurrency))
        {
            throw new InvalidOperationException($"Restricted currencies cannot be used in conversion");
        }

        var provider = _providerFactory.GetProvider();
        var cacheKey = CacheKeys.ConversionRate(request.SourceCurrency, request.TargetCurrency);

        if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse conversionData))
        {
            _logger.LogInformation("Cache miss for conversion from {SourceCurrency} to {TargetCurrency}",
                request.SourceCurrency, request.TargetCurrency);

            conversionData = await provider.PerformConversionAsync(1, request.SourceCurrency, request.TargetCurrency);

            _cache.Set(cacheKey, conversionData, TimeSpan.FromHours(1));
        }
        else
        {
            _logger.LogInformation("Cache hit for conversion from {SourceCurrency} to {TargetCurrency}",
                request.SourceCurrency, request.TargetCurrency);
        }

        return _mapper.Map<ValutaCoreResponse>((
            Source: conversionData,
            Amount: request.Amount,
            FromCurrency: request.SourceCurrency,
            ToCurrency: request.TargetCurrency
        ));
    }

    public async Task<PaginatedResponse<RateHistoryEntry>> GetHistoricalRatesAsync(HistoricalRatesRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrEmpty(request.BaseCurrency))
        {
            throw new ArgumentException("Base currency must be specified", nameof(request.BaseCurrency));
        }

        if (_restrictedCurrencies.Contains(request.BaseCurrency))
        {
            throw new InvalidOperationException($"Currency {request.BaseCurrency} is restricted and cannot be used");
        }

        if (request.StartDate > request.EndDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        var cacheKey = CacheKeys.HistoricalRates(
            request.BaseCurrency,
            request.StartDate.ToString("yyyy-MM-dd"),
            request.EndDate.ToString("yyyy-MM-dd"));

        if (!_cache.TryGetValue(cacheKey, out Dictionary<DateTime, Dictionary<string, decimal>> historicalData))
        {
            _logger.LogInformation(
                "Cache miss for historical rates with base currency {BaseCurrencyCode} from {StartDate} to {EndDate}",
                request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));

            var provider = _providerFactory.GetProvider();
            historicalData =
                await provider.RetrieveHistoricalRatesAsync(request.BaseCurrency, request.StartDate, request.EndDate);

            _cache.Set(cacheKey, historicalData, TimeSpan.FromHours(24));
        }
        else
        {
            _logger.LogInformation(
                "Cache hit for historical rates with base currency {BaseCurrencyCode} from {StartDate} to {EndDate}",
                request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));
        }

        foreach (var date in historicalData?.Keys!)
        {
            foreach (var restrictedCurrency in _restrictedCurrencies)
            {
                historicalData[date].Remove(restrictedCurrency);
            }
        }

        var allRates = historicalData
            .Select(kvp => new RateHistoryEntry
            {
                Date = kvp.Key,
                BaseCurrencyCode = request.BaseCurrency,
                ExchangeRates = kvp.Value
            })
            .OrderByDescending(r => r.Date)
            .ToList();

        return _mapper.Map<PaginatedResponse<RateHistoryEntry>>((Request: request, AllRates: allRates));
    }
}