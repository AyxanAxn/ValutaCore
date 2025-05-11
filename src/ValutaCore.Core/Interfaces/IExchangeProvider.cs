namespace ValutaCore.Core.Interfaces;

public interface IExchangeProvider
{
    string ProviderIdentifier { get; }
    Task<ExchangeRateResponse> RetrieveLatestRatesAsync(string baseCurrency);
    Task<ExchangeRateResponse> PerformConversionAsync(decimal amount, string fromCurrency, string toCurrency);

    Task<Dictionary<DateTime, Dictionary<string, decimal>>> RetrieveHistoricalRatesAsync(string baseCurrency,
        DateTime startDate, DateTime endDate);
}