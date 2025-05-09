namespace ValutaCore.Core.Services;

public class ValutaService(
    IExchangeProviderFactory providerFactory,
    IMemoryCache cache,
    ILogger<ValutaService> logger,
    IMapper mapper)
    : IValutaService
{
    private readonly IExchangeProviderFactory _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
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
        ValidateBaseCurrency(baseCurrency);

        var cacheKey = CacheKeys.LatestRates(baseCurrency);
        
        if (_cache.TryGetValue(cacheKey, out ExchangeRateResponse? rates))
        {
            _logger.LogDebug("Cache hit: Retrieved latest rates for base currency {BaseCurrency}", baseCurrency);
            return rates!;
        }

        _logger.LogInformation("Fetching latest rates for base currency {BaseCurrency}", baseCurrency);
        
        var provider = _providerFactory.GetProvider();
        rates = await provider.RetrieveLatestRatesAsync(baseCurrency);
        
        RemoveRestrictedCurrencies(rates.Rates);
        
        _cache.Set(cacheKey, rates, TimeSpan.FromHours(1));
        
        return rates;
    }

    public async Task<ValutaCoreResponse> ConvertCurrencyAsync(ExchangeRequest request)
    {
        ValidateConversionRequest(request);

        var sourceCurrency = request.SourceCurrency.ToUpperInvariant();
        var targetCurrency = request.TargetCurrency.ToUpperInvariant();
        var cacheKey = CacheKeys.ConversionRate(sourceCurrency, targetCurrency);

        if (_cache.TryGetValue(cacheKey, out ExchangeRateResponse conversionData))
        {
            _logger.LogDebug("Cache hit: Retrieved conversion rate from {SourceCurrency} to {TargetCurrency}", 
                sourceCurrency, targetCurrency);
        }
        else
        {
            _logger.LogInformation("Fetching conversion rate from {SourceCurrency} to {TargetCurrency}", 
                sourceCurrency, targetCurrency);
                
            var provider = _providerFactory.GetProvider();
            conversionData = await provider.PerformConversionAsync(1, sourceCurrency, targetCurrency);
            
            _cache.Set(cacheKey, conversionData, TimeSpan.FromHours(1));
        }

        return _mapper.Map<ValutaCoreResponse>((
            Source: conversionData,
            Amount: request.Amount,
            FromCurrency: sourceCurrency,
            ToCurrency: targetCurrency
        ));
    }

    public async Task<PaginatedResponse<RateHistoryEntry>> GetHistoricalRatesAsync(HistoricalRatesRequest request)
    {
        ValidateHistoricalRequest(request);
        NormalizeHistoricalRequest(request);

        var cacheKey = CacheKeys.HistoricalRates(
            request.BaseCurrency,
            request.StartDate.ToString("yyyy-MM-dd"),
            request.EndDate.ToString("yyyy-MM-dd"));

        Dictionary<DateTime, Dictionary<string, decimal>> historicalData;
        
        if (_cache.TryGetValue(cacheKey, out historicalData!))
        {
            _logger.LogDebug("Cache hit: Retrieved historical rates for {BaseCurrency} from {StartDate} to {EndDate}",
                request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));
        }
        else
        {
            _logger.LogInformation("Fetching historical rates for {BaseCurrency} from {StartDate} to {EndDate}",
                request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));
                
            var provider = _providerFactory.GetProvider();
            historicalData = await provider.RetrieveHistoricalRatesAsync(
                request.BaseCurrency, request.StartDate, request.EndDate);
                
            _cache.Set(cacheKey, historicalData, TimeSpan.FromHours(24));
        }

        RemoveRestrictedCurrenciesFromHistoricalData(historicalData!);
        
        var allRates = ConvertToRateHistoryEntries(historicalData, request.BaseCurrency);
        
        return _mapper.Map<PaginatedResponse<RateHistoryEntry>>((Request: request, AllRates: allRates));
    }

    #region Private Helper Methods
    
    private void ValidateBaseCurrency(string baseCurrency)
    {
        if (string.IsNullOrEmpty(baseCurrency))
        {
            throw new ArgumentException("Base currency cannot be null or empty", nameof(baseCurrency));
        }

        if (_restrictedCurrencies.Contains(baseCurrency))
        {
            throw new InvalidOperationException($"Currency {baseCurrency} is restricted and cannot be used");
        }
    }
    
    private void ValidateConversionRequest(ExchangeRequest request)
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
            throw new InvalidOperationException("Restricted currencies cannot be used in conversion");
        }
    }
    
    private void ValidateHistoricalRequest(HistoricalRatesRequest request)
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
    }
    
    private void NormalizeHistoricalRequest(HistoricalRatesRequest request)
    {
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }
    }
    
    private void RemoveRestrictedCurrencies(Dictionary<string, decimal> rates)
    {
        foreach (var restrictedCurrency in _restrictedCurrencies)
        {
            rates.Remove(restrictedCurrency);
        }
    }
    
    private void RemoveRestrictedCurrenciesFromHistoricalData(Dictionary<DateTime, Dictionary<string, decimal>> historicalData)
    {
        foreach (var date in historicalData.Keys)
        {
            foreach (var restrictedCurrency in _restrictedCurrencies)
            {
                historicalData[date].Remove(restrictedCurrency);
            }
        }
    }
    
    private List<RateHistoryEntry> ConvertToRateHistoryEntries(
        Dictionary<DateTime, Dictionary<string, decimal>> historicalData, 
        string baseCurrency)
    {
        return historicalData
            .Select(kvp => new RateHistoryEntry
            {
                Date = kvp.Key,
                BaseCurrencyCode = baseCurrency,
                ExchangeRates = kvp.Value
            })
            .OrderByDescending(r => r.Date)
            .ToList();
    }
    #endregion
}