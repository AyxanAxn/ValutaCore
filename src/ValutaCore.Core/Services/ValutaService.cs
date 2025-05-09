namespace ValutaCore.Core.Services;

public class ValutaService(
    IExchangeProviderFactory providerFactory,
    IMemoryCache cache,
    ILogger<ValutaService> logger,
    IMapper mapper,
    HashSet<string> restrictedCurrencies)
    : IValutaService
{
    private readonly IExchangeProviderFactory _providerFactory =
        providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));

    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<ValutaService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    private readonly HashSet<string> _restricted =
        restrictedCurrencies ?? throw new ArgumentNullException(nameof(restrictedCurrencies));

    public async Task<ExchangeRateResponse> GetLatestRatesAsync(
        string baseCurrency)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
            throw new ArgumentException("Base currency cannot be empty.", nameof(baseCurrency));
        if (_restricted.Contains(baseCurrency))
            throw new InvalidOperationException($"Currency '{baseCurrency}' is restricted.");

        var cacheKey = CacheKeys.LatestRates(baseCurrency);
        if (_cache.TryGetValue(cacheKey, out ExchangeRateResponse cached))
        {
            _logger.LogDebug("Cache hit -> {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogInformation("Cache miss -> {CacheKey}", cacheKey);
        var provider = _providerFactory.GetProvider();
        var fresh = await provider.RetrieveLatestRatesAsync(baseCurrency);

        // remove forbidden codes
        foreach (var code in _restricted)
            fresh.Rates.Remove(code);

        _cache.Set(cacheKey, fresh, TimeSpan.FromHours(1));
        return fresh;
    }

    public async Task<ValutaCoreResponse> ConvertCurrencyAsync(
        ExchangeRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.SourceCurrency) ||
            string.IsNullOrWhiteSpace(request.TargetCurrency))
            throw new ArgumentException("Source and target currencies must be specified.");
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));
        if (_restricted.Contains(request.SourceCurrency) ||
            _restricted.Contains(request.TargetCurrency))
            throw new InvalidOperationException("Restricted currencies cannot be used in conversion.");

        var from = request.SourceCurrency.ToUpperInvariant();
        var to = request.TargetCurrency.ToUpperInvariant();
        var cacheKey = CacheKeys.ConversionRate(from, to);

        if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse rate1To1))
        {
            _logger.LogInformation("Cache miss -> {CacheKey}", cacheKey);
            var provider = _providerFactory.GetProvider();
            rate1To1 = await provider.PerformConversionAsync(1, from, to);
            _cache.Set(cacheKey, rate1To1, TimeSpan.FromHours(1));
        }
        else
        {
            _logger.LogDebug("Cache hit -> {CacheKey}", cacheKey);
        }

        return _mapper.Map<ValutaCoreResponse>((rate1To1, request.Amount, from, to));
    }

    public async Task<PaginatedResponse<RateHistoryEntry>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.BaseCurrency))
            throw new ArgumentException("Base currency must be specified.", nameof(request.BaseCurrency));
        if (_restricted.Contains(request.BaseCurrency))
            throw new InvalidOperationException($"Currency '{request.BaseCurrency}' is restricted.");
        if (request.StartDate > request.EndDate)
            throw new ArgumentException("StartDate cannot be after EndDate.");

        request.Page = Math.Max(request.Page, 1);
        request.PageSize = Math.Max(request.PageSize, 1);

        var start = request.StartDate.ToString("yyyy-MM-dd");
        var end = request.EndDate.ToString("yyyy-MM-dd");
        var cacheKey = CacheKeys.HistoricalRates(request.BaseCurrency, start, end);

        if (!_cache.TryGetValue(cacheKey, out Dictionary<DateTime, Dictionary<string, decimal>> hist))
        {
            _logger.LogInformation("Cache miss -> {CacheKey}", cacheKey);
            var provider = _providerFactory.GetProvider();
            hist = await provider.RetrieveHistoricalRatesAsync(
                request.BaseCurrency, request.StartDate, request.EndDate);
            _cache.Set(cacheKey, hist, TimeSpan.FromHours(24));
        }
        else
        {
            _logger.LogDebug("Cache hit -> {CacheKey}", cacheKey);
        }

        // strip out restricted codes
        foreach (var day in hist.Keys)
        foreach (var code in _restricted)
            hist[day].Remove(code);

        var allRates = hist
            .Select(kvp => new RateHistoryEntry
            {
                Date = kvp.Key,
                BaseCurrencyCode = request.BaseCurrency,
                ExchangeRates = kvp.Value
            })
            .OrderByDescending(r => r.Date)
            .ToList();

        return _mapper.Map<PaginatedResponse<RateHistoryEntry>>((request, allRates));
    }
}